#!/bin/sh

git checkout --track remotes/origin/feature/elephant-tree-interaction
git submodule init
git submodule update
cd model/model-mars-knp/
git checkout --track remotes/origin/model/skukuza-ulfia
cd ../model-savanna-trees/
git branch -d master
git checkout --track remotes/origin/master