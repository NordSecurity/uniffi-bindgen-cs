#!/bin/bash
set -euxo pipefail

# "Using" statements musn't appear in generated code, but they are required
# when compiling the file for tests. To get around this, simply prepend "Using"
# statements to the actual file content.
TMP_STREAM_FILE="dotnet-tests/UniffiCS.tests/gen/tmp_BigEndianStream.cs"

mkdir -p $(dirname "$TMP_STREAM_FILE")
echo "using System;" > $TMP_STREAM_FILE
echo "using System.IO;" >> $TMP_STREAM_FILE
cat bindgen/templates/BigEndianStream.cs >> $TMP_STREAM_FILE
sed -i 's/{#//g' $TMP_STREAM_FILE
sed -i 's/#}//g' $TMP_STREAM_FILE

cd dotnet-tests/UniffiCS.tests
dotnet test -l "console;verbosity=normal"
