# /bin/bash -ex
rm -rf _publish
dotnet clean
dotnet publish  -o _publish
