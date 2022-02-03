#!/bin/sh

cd /home/dave

cat <<EOT >> 20auto-upgrades
APT::Periodic::Update-Package-Lists "0";
APT::Periodic::Download-Upgradeable-Packages "0";
APT::Periodic::AutocleanInterval "0";
APT::Periodic::Unattended-Upgrade "1";
EOT

sudo mv /home/dave/20auto-upgrades /etc/apt/apt.conf.d/20auto-upgrades

# go with newer apt which gets dependency updates too (like linux-azure)
sudo apt update -y
sudo apt dist-upgrade -y

sudo apt install python3-pip -y

sudo apt install libsndfile1 -y

sudo apt install libsndfile1-dev -y

# to stop errors
# or append to ~/.bashrc
#  WARNING: The scripts f2py, f2py3 and f2py3.8 are installed in '/home/dave/.local/bin' which is not on PATH.
#  Consider adding this directory to PATH or, if you prefer to suppress this warning, use --no-warn-script-location.
export PATH=/home/dave/.local/bin:$PATH


# pip is a soft link and should point to Python3 in our case
# https://stackoverflow.com/questions/40832533/pip-or-pip3-to-install-packages-for-python-3
pip install torchaudio -f https://download.pytorch.org/whl/torch_stable.html

pip install soundfile

# install numpy to get rid of this error
pip install -U numpy

sudo apt install ffmpeg -y

# STOP for image
# sudo git clone https://github.com/spatial-intelligence/OSR4Rights.git
# cd OSR4Rights
# sudo chmod 777 AudioTools
# cd AudioTools

# # mkdir input
# sudo chmod 777 input

# # make it easy for user dave so can run ./encodeToWAV.sh while testing (and maybe in prod)
# sudo chmod 777 encodeToWAV.sh
# sudo chmod 777 run.sh

# # SAMPLES
# # get sample wave files using yt-dlp from youtube
# # I've zipped some up here so can download quickly to run perf tests
# cd input
# wget https://functionsdm2storage.blob.core.windows.net/outputfiles/8mixFormats.zip
# sudo apt install unzip
# unzip 8mixFormats.zip

# # after this rm I have 7.4GB free disk
# rm 8mixFormats.zip

# sudo snap install btop

# # encode all files in input diretory mp3's etc to WAV
# cd ..
# ./encodeToWAV.sh

# get speech parts
# ./run.sh



# python3 audiofile_reduce_to_speechparts.py -i /home/dave/OSR4Rights/AudioTools/input/ -j 123  


