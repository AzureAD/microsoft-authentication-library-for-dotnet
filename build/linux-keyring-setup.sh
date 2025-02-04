#!/bin/bash

echo "Setting DBUS_SESSION_BUS_ADDRESS"
echo "Set to DBUS_SESSION_BUS_ADDRESS=${DBUS_SESSION_BUS_ADDRESS}"

killall -q -u "$(whoami)" gnome-keyring-daemon
echo "gnome-keyring-daemon was terminated"

rm -f ~/.local/share/keyrings/login.keyring
echo "Login keyring deleted"

_UNLOCK_KEYRING_DATA=`cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 10 | head -n 1`
echo "_UNLOCK_KEYRING_DATA is set"

eval $(echo -n "${_UNLOCK_KEYRING_DATA}" \
| gnome-keyring-daemon --daemonize --login \
| sed -e 's/^/export /')
echo "keyring daemon was set"

unset _UNLOCK_KEYRING_DATA
/usr/bin/gnome-keyring-daemon --start --components=secrets
echo "keyring daemon started"

secret-tool search --all version 1.0
echo "secret-tool executed"

echo "##vso[task.setvariable variable=keyRingControl;isOutput=true]$GNOME_KEYRING_CONTROL"
echo "GNOME_KEYRING_CONTROL was set."
