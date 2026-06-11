#!/bin/bash
# ============================================================
# build-mac-app.sh
# Crea un .app bundle Mac pronto per distribuzione.
#
# Uso:
#   ./build-mac-app.sh                    # build per Apple Silicon (default)
#   ./build-mac-app.sh osx-x64             # build per Intel
#   ./build-mac-app.sh osx-arm64           # build per Apple Silicon esplicito
# ============================================================

set -e  # Exit on error

# Configurazione
APP_NAME="SmartComputerControl"
BUNDLE_ID="com.sergio.smartcomputercontrol"
RID="${1:-osx-arm64}"   # default arm64
OUTPUT_DIR="./dist"
APP_DIR="$OUTPUT_DIR/$APP_NAME.app"

echo "📦 Building $APP_NAME for $RID..."

# 1. Pubblica con dotnet (self-contained, single file)
echo "→ Compilazione .NET..."
dotnet publish -f net10.0 -c Release -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:EnableWindowsTargeting=true \
    -o "$OUTPUT_DIR/publish-tmp"

# 2. Pulisce eventuale bundle precedente
rm -rf "$APP_DIR"

# 3. Crea la struttura del bundle
echo "→ Creazione struttura .app bundle..."
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# 4. Copia Info.plist
cp Info.plist "$APP_DIR/Contents/Info.plist"

# 5. Copia l'icona
cp Assets/scc-icon.icns "$APP_DIR/Contents/Resources/scc-icon.icns"

# 6. Copia tutti i file pubblicati nel MacOS folder
cp -R "$OUTPUT_DIR/publish-tmp/"* "$APP_DIR/Contents/MacOS/"

# 7. Rinomina l'eseguibile (dotnet lo chiama "SmartComputerControl" senza estensione, ok)
chmod +x "$APP_DIR/Contents/MacOS/$APP_NAME"

# 8. Cleanup
rm -rf "$OUTPUT_DIR/publish-tmp"

# 9. Codesign ad-hoc (richiesto da macOS moderno anche senza Apple Developer)
echo "→ Firma ad-hoc..."
codesign --force --deep --sign - "$APP_DIR"

echo "✅ Bundle creato: $APP_DIR"
echo ""
echo "Per testare: open '$APP_DIR'"
echo "Per installare: trascina '$APP_DIR' nella cartella Applicazioni"
