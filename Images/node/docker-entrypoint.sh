#!/bin/sh
#------------------------------------------------------------------------------
# FILE:         docker-entrypoint.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.
#
# Loads the Docker host node environment variables before launching the Node
# application.

# Load the host node environment.

if [ -f /etc/neon/host-env ] ; then
    . /etc/neon/host-env
fi

set -e
node /program.js
