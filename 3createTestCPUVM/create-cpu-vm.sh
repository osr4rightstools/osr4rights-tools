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

# to stop errorso
# or append to ~/.bashrc
#  WARNING: The scripts f2py, f2py3 and f2py3.8 are installed in '/home/dave/.local/bin' which is not on PATH.
#  Consider adding this directory to PATH or, if you prefer to suppress this warning, use --no-warn-script-location.
export PATH=/home/dave/.local/bin:$PATH

pip3 install numpy
pip3 install nltk
pip3 install pandas
pip3 install keras
pip3 install testresources
pip3 install tensorflow
pip3 install scikit-learn
pip3 install preprocessor
pip3 install textblob
pip3 install transformers

pip3 install sentencepiece

sudo -u dave python3 -m nltk.downloader stopwords
sudo -u dave python3 -m nltk.downloader punkt

sudo -u dave python3 -m textblob.download_corpora

sudo apt install unzip -y

# Hatespeech source
# sudo git clone https://github.com/khered20/Prepared_HateSpeech_Models /home/dave/hatespeech

# sudo chown -R dave:dave /home/dave/hatespeech 
# sudo chmod +x /home/dave/hatespeech

# # Prepared model
# cd /home/dave/hatespeech
# wget https://functionsdm2storage.blob.core.windows.net/outputfiles/_all_train_results-20210712T152424Z-001.zip
# unzip _all_train_results-20210712T152424Z-001.zip -d /home/dave/hatespeech

# cd /home/dave/hatespeech

# 14th July
#  1.TE1.csh
# Run the tool as dave to get the 1.9GB data file cached??
# sudo -u dave python3 PreBERT.py -m xlm-roberta-base -d all_train -s TE1.csv -fn hate_speech_results

# 2.TESTfile.csv - 300 tweets
# python3 PreBERT.py -m xlm-roberta-base -d all_train -s TESTfile.csv -fn hate_speech_results

# 3.multilingual_test.csv
# python3 PreBERT.py -m xlm-roberta-base -d all_train -s multilingual_test.csv -fn hate_speech_results


## Monitoring Tools

# sudo snap install bpytop