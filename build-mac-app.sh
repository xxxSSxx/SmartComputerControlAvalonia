#!/bin/bash
# ============================================================
# build-mac-app.sh
# Crea un .app bundle Mac pronto per distribuzione.
# ============================================================

set -e

APP_NAME="SmartComputerControl"
BUNDLE_ID="com.sergio.smartcomputercontrol"
RID="${1:-osx-arm64}"
OUTPUT_DIR="./dist"
APP_DIR="$OUTPUT_DIR/$APP_NAME.app"

echo "📦 Building $APP_NAME for $RID..."

echo "→ Compilazione .NET..."
dotnet publish -f net10.0 -c Release -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:EnableWindowsTargeting=true \
    -o "$OUTPUT_DIR/publish-tmp"

rm -rf "$APP_DIR"

echo "→ Creazione struttura .app bundle..."
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

cp Info.plist "$APP_DIR/Contents/Info.plist"
cp Assets/scc-icon.icns "$APP_DIR/Contents/Resources/scc-icon.icns"
cp -R "$OUTPUT_DIR/publish-tmp/"* "$APP_DIR/Contents/MacOS/"
chmod +x "$APP_DIR/Contents/MacOS/$APP_NAME"

rm -rf "$OUTPUT_DIR/publish-tmp"

echo "→ Firma ad-hoc..."
codesign --force --deep --sign - "$APP_DIR"

# Refresh della cache LaunchServices per forzare macOS a rileggere Info.plist
# (cruciale per evitare che vecchie configurazioni siano riusate)
echo "→ Refresh cache LaunchServices..."
LSREGISTER="/System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/LaunchServices.framework/Versions/A/Support/lsregister"
if [ -x "$LSREGISTER" ]; then
    "$LSREGISTER" -f "$APP_DIR" 2>/dev/null || true
fi

# Verifica che LSUIElement sia presente nel plist installato
echo "→ Verifica Info.plist:"
plutil -p "$APP_DIR/Contents/Info.plist" | grep -E "LSUIElement|CFBundleIdentifier" || true

echo ""
echo "✅ Bundle creato: $APP_DIR"
echo ""
echo "Per testare: open '$APP_DIR'"
echo "Per installare: trascina '$APP_DIR' nella cartella Applicazioni"
echo ""
echo "⚠️ IMPORTANTE: se hai gia' una versione installata in /Applicazioni,"
echo "   prima cancellala completamente e svuota il cestino, POI installa la nuova"
echo "   (macOS fa caching aggressivo sulle preferenze app)."
