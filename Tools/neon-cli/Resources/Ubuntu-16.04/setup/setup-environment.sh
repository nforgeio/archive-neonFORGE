#!/bin/bash
#------------------------------------------------------------------------------
# FILE:         setup-environment.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.
#
# NOTE: This script must be run under [sudo].
#
# NOTE: Variables formatted like $<name> will be expanded by [neon-cli]
#       using a [PreprocessReader].
#
# This script manages the global environment variables stored in [/etc/environment].
# The commands are:
#
#       setup-environment.sh set NAME VALUE
#       setup-environment.sh remove NAME
#       setup-environment.sh remove-regex REGEX
#
# Note: A reboot is required for changes to take effect.
#
# The [set] command changes the value of an existing variable or
# adds a new one.
#
# The [remove] command removes a variable if it exists.
#
# The [remove-regex] removes variables whose names match a REGEX
# pattern.

environmentPath=/etc/environment

mkdir -p ${HOME}/temp
tempPath=${HOME}/temp/environment-`date --utc +%s-%N`.log

if [[ ${1} ]] ; then
    command=${1}
else
    command=
fi

# Implement the command.

case ${command} in

set)

    if [[ ${2} ]] ; then
        name=${2}
    else
        echo "ERROR[setup-environment]: NAME argument is required." >&2
        exit 1
    fi

    if [[ ${3} ]] ; then
        value=${3}
    else
        value=""
    fi

    regex="^${name}=.*$"
    found=false

    while IFS='' read -r line || [[ -n "${line}" ]]; 
    do
        if [[ ${line} =~ ${regex} ]] ; then
            echo "${name}=${value}" >> ${tempPath}
            found=true
        else
            echo ${line} >> ${tempPath}
        fi

    done < ${environmentPath}

    if ! ${found} ; then
        echo "${name}=${value}" >> ${tempPath}
    fi
    ;;

remove)

    if [[ ${2} ]] ; then
        name=${2}
    else
        echo "ERROR[setup-environment]: NAME argument is required." >&2
        exit 1
    fi

    regex="^${name}=.*$"

    while IFS='' read -r line || [[ -n "${line}" ]]; 
    do
        if ! [[ ${line} =~ ${regex} ]] ; then
            echo ${line} >> ${tempPath}
        fi

    done < ${environmentPath}
    ;;

remove-regex)

    if [[ ${2} ]] ; then
        regex=${2}
    else
        echo "ERROR[setup-environment]: REGEX argument is required." >&2
        exit 1
    fi

    regex="^${regex}=.*$"

    while IFS='' read -r line || [[ -n "${line}" ]]; 
    do
        if ! [[ ${line} =~ ${regex} ]] ; then
            echo ${line} >> ${tempPath}
        fi

    done < ${environmentPath}
    ;;

*)

    echo "ERROR[setup-environment]: Unknown command [${1}]." >&2
    exit 1
    ;;

esac

# Overwrite [/etc/environment] with the generated temporary file
# end then remove the temp file.

mv ${tempPath} ${environmentPath}
rm -f ${tempPath}

exit 0
