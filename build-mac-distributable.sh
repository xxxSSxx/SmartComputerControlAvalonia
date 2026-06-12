#!/bin/bash
# =============================================================
# build-mac-distributable.sh
# Crea i pacchetti .dmg per Mac (Apple Silicon + Intel) pronti
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

    local DMG_NAME="${APP_NAME}-${VERSION}-mac-${LABEL}.dmg"
    local DMG_PATH="$DIST_DIR/$DMG_NAME"
    local STAGING_DIR="$DIST_DIR/staging-$LABEL"

    # Prepara cartella temporanea con app + alias Applicazioni
    mkdir -p "$STAGING_DIR"
    cp -R "./dist/$APP_NAME.app" "$STAGING_DIR/"
    # Crea alias simbolico verso /Applications dentro il DMG
    ln -s /Applications "$STAGING_DIR/Applications"

    # Crea il DMG
    # -volname: nome del volume che apparira' quando l'utente fa doppio click
    # -srcfolder: contenuto da mettere nel DMG
    # -ov: overwrite se esiste gia'
    # -format UDZO: compressione standard (zlib)
    echo "→ Creazione DMG..."
    hdiutil create \
        -volname "$APP_NAME" \
        -srcfolder "$STAGING_DIR" \
        -ov \
        -format UDZO \
        "$DMG_PATH" > /dev/null

    rm -rf "$STAGING_DIR"

    echo "✅ Creato: $DMG_PATH"
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
echo "  - SmartComputerControl-${VERSION}-mac-arm64.dmg → per Mac Apple Silicon (M1+)"
echo "  - SmartComputerControl-${VERSION}-mac-intel.dmg → per Mac Intel"
