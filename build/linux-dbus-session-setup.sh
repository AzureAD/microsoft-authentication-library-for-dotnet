#!/bin/bash

echo "Setting DBUS_SESSION_BUS_ADDRESS"
sudo chown -R root /mnt/wslg/runtime-dir

sudo dbus-uuidgen --ensure
eval `dbus-launch --sh-syntax`

echo "dbus-launch finished"

echo "##vso[task.setvariable variable=dbusSessionAddress;isOutput=true]$DBUS_SESSION_BUS_ADDRESS"
echo "Set dbusSessionAddress successfully"
echo "set DBUS_SESSION_BUS_ADDRESS=${DBUS_SESSION_BUS_ADDRESS}"
