#!/bin/sh

# Creates Ubuntu webserver to run osr4rightstools.org

# Ubuntu 26.04 LTS
# daily unattended security upgrades (keeps nginx patched between deploys)
cd /home/dave

cat <<EOT > 20auto-upgrades
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Download-Upgradeable-Packages "1";
APT::Periodic::AutocleanInterval "7";
APT::Periodic::Unattended-Upgrade "1";
EOT

sudo mv /home/dave/20auto-upgrades /etc/apt/apt.conf.d/20auto-upgrades

sudo apt-get update -y
sudo DEBIAN_FRONTEND=noninteractive apt-get upgrade -y

# nginx from the Ubuntu archive (Ubuntu backports security fixes)
sudo apt-get install -y nginx

# fail if nginx is older than the nginx2root fix (USN-9159-1) for 26.04
nginx_version=$(dpkg-query -W -f='${Version}' nginx)
if ! dpkg --compare-versions "$nginx_version" ge 1.28.3-2ubuntu1.13; then
  echo "nginx $nginx_version is not patched for nginx2root - stopping"
  exit 1
fi

# .NET 10 SDK from the Ubuntu archive
sudo apt-get install -y dotnet-sdk-10.0

# SSL cert from Let's Encrypt via DNS-01 challenge on DNSimple
# certbot snap installs a systemd timer which auto renews
sudo snap wait system seed.loaded
sudo snap install --classic certbot
sudo snap set certbot trust-plugin-with-root=ok
sudo snap install certbot-dns-dnsimple

# token was scp'd to /home/dave by infra.azcli
sudo mkdir -p /etc/letsencrypt
echo "dns_dnsimple_token = $(head -n 1 /home/dave/dnsimpletoken.txt)" | sudo tee /etc/letsencrypt/dnsimple.ini > /dev/null
sudo chmod 600 /etc/letsencrypt/dnsimple.ini
rm /home/dave/dnsimpletoken.txt

sudo certbot certonly --non-interactive --agree-tos --register-unsafely-without-email \
  --dns-dnsimple --dns-dnsimple-credentials /etc/letsencrypt/dnsimple.ini \
  -d osr4rightstools.org -d www.osr4rightstools.org \
  --deploy-hook "systemctl reload nginx"

# create document root for published files
sudo mkdir /var/www

# create gitsource folder and clone
sudo mkdir /gitsource
cd /gitsource
sudo git clone https://github.com/osr4rightstools/osr4rights-tools .

# nginx config
# ssl certs are in /etc/letsencrypt/live/osr4rightstools.org/ from certbot above
sudo cp /gitsource/infra/nginx.conf /etc/nginx/sites-available/default
sudo nginx -s reload

# compile and publish the web app
sudo dotnet publish /gitsource/src/OSR4Rights.Web --configuration Release --output /var/www

# change ownership of the published files to what it will run under
sudo chown -R www-data:www-data /var/www
# allow exective permissions
sudo chmod +x /var/www

# cookie keys to allow machine to restart and for it to 'remember' cookies
# todo - store these in blob storage?
sudo mkdir /var/osr-cookie-keys
sudo chown -R www-data:www-data /var/osr-cookie-keys
# allow read and write
sudo chmod +rw /var/osr-cookie-keys

# auto start on machine reboot
sudo systemctl enable kestrel-osr.service

# start the Kestrel web app using systemd using kestrel-osr.service text files
sudo systemctl start kestrel-osr.service

# https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-5.0#configure-the-firewall
sudo apt-get install ufw

sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

sudo ufw enable

  # a nice shortcut sym link
# sudo ln -s /usr/local/openresty/nginx/ /home/dave/nginx
sudo ln -s /var/www/logs/ /home/dave/logs

# sudo snap install bpytop

