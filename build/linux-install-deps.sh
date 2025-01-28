#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
apt install sudo
# This script must be run elevated. Adding a sudo wrapper if needed.
if [ "$UID" -ne 0 ]; then
    exec sudo "$0" "$@"
fi

set -o errexit   # Exit the script if any command returns a non-true return value

if [ -f '/usr/bin/apt' ]; then
    DEBIAN_FRONTEND=noninteractive
    # Install quietly, accepting all packages and not overriding user configurations
    PKGINSTALL_CMD='apt-get install -q -y -o Dpkg::Options::=--force-confold'
    PACKAGE_MANAGER=apt
    PKGEXISTS_CMD='dpkg -s'
elif [ -f '/usr/bin/yum' ]; then
    PACKAGE_MANAGER=yum
    PKGINSTALL_CMD='yum -y install'
    PKGEXISTS_CMD='yum list installed'
else
    echo 'Package system currently not supported.'
    exit 2
fi

if [ $PACKAGE_MANAGER == 'apt' ]; then
    apt-get update || true # If apt update fails, see if we can continue anyway
    $PKGINSTALL_CMD \
        libx11-dev \
        dbus-x11 \
        libsystemd0 \
        x11-xserver-utils \
        libp11-kit-dev \
        libwebkit2gtk-4.0-dev
fi

echo "Installing JavaBroker"
LINUX_VERSION=$(sed -r -n -e 's/^VERSION_ID="?([^"]+)"?/\1/p' /etc/os-release)
LINUX_VERSION_MAIN=$(echo $LINUX_VERSION | sed 's/\([0-9]*\)\..*/\1/')

if [ -f '/usr/bin/apt' ]; then
    curl https://packages.microsoft.com/config/ubuntu/$LINUX_VERSION/prod.list | sudo sudo tee /etc/apt/trusted.gpg.d/microsoft.asc
else
    $PKGINSTALL_CMD yum-utils
    yum-config-manager --add-repo=https://packages.microsoft.com/config/rhel/$LINUX_VERSION_MAIN/prod.repo
    rpm --import http://packages.microsoft.com/keys/microsoft.asc
fi
echo "Installing latest published JavaBroker package"
$PKGINSTALL_CMD $BROKER_PACKAGE_NAME

exit 0