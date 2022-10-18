#!/bin/bash

BACKUP_DIR="/home/dave/"
FILE_NAME=$BACKUP_DIR`date +%d-%m-%Y-%I-%M-%S-%p`.backup

pg_dump -Fc -U postgres nasafiremap > $FILE_NAME