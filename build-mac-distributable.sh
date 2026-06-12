#!/bin/bash
# =============================================================
# build-mac-distributable.sh
# Crea i pacchetti .zip per Mac (Apple Silicon + Intel) pronti
# per essere caricati su un sito web.
# =============================================================

set -e

APP_NAME="SmartComputerControl"
VERSION="1.0.0"
DIST_DIR="./dist-mac"

# Pulisci output precedente
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

build_arch() {
    local RID="$1"
    local LABEL="$2"

    echo ""
    echo "============================================================"
    echo "📦 Building per $LABEL ($RID)..."
    echo "============================================================"

    # Usa lo script esistente che crea il bundle
    ./build-mac-app.sh "$RID"

    # Sposta il bundle nella cartella distrib con nome architettura
    local ZIPNAME="${APP_NAME}-${VERSION}-mac-${LABEL}.zip"
    local TMPDIR="$DIST_DIR/tmp-$LABEL"
    mkdir -p "$TMPDIR"
    cp -R "./dist/$APP_NAME.app" "$TMPDIR/"

    # Zip mantenendo i permessi/symlink del bundle
    cd "$TMPDIR"
    zip -ry "../$ZIPNAME" "$APP_NAME.app"
    cd - > /dev/null
    rm -rf "$TMPDIR"

    echo "✅ Creato: $DIST_DIR/$ZIPNAME"
}

# Build per Apple Silicon (M1/M2/M3/M4)
build_arch "osx-arm64" "arm64"

# Build per Intel
build_arch "osx-x64" "intel"

# Pulisci dist temporanea
rm -rf ./dist

echo ""
echo "============================================================"
echo "✅ Tutti i pacchetti Mac creati in $DIST_DIR/"
ls -lh "$DIST_DIR"
echo "============================================================"
echo ""
echo "Puoi caricare questi file sul tuo sito web:"
echo "  - SmartComputerControl-${VERSION}-mac-arm64.zip → per Mac Apple Silicon (M1+)"
echo "  - SmartComputerControl-${VERSION}-mac-intel.zip → per Mac Intel"
