#!/bin/sh

BUILD_PROJ_DIR=$1
BUILD_TARGET_DIR=$2

CONTENTS_DIR=$BUILD_TARGET_DIR/DesignArsenal.app/Contents
MONOBUNDLE=$BUILD_TARGET_DIR/DesignArsenal.app/Contents/MonoBundle

rm -rf $MONOBUNDLE/*.pdb
