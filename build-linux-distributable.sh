#!/bin/bash
# =============================================================
# build-linux-distributable.sh
# Crea un .tar.gz Linux x64 pronto per essere distribuito.
# =============================================================

set -e

APP_NAME="SmartComputerControl"
VERSION="1.0.0"
DIST_DIR="./dist-linux"
PUBLISH_DIR="$DIST_DIR/$APP_NAME"

# Pulisci
rm -rf "$DIST_DIR"
mkdir -p "$PUBLISH_DIR"

echo "📦 Building SmartComputerControl per Linux x64..."

# 1. Publish
dotnet publish -f net10.0 -c Release -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:EnableWindowsTargeting=true \
    -o "$PUBLISH_DIR"

# 2. Rimuovi PDB e file di debug (non servono al runtime)
find "$PUBLISH_DIR" -name "*.pdb" -delete

# 3. File .desktop per integrazione con desktop environment
cat > "$PUBLISH_DIR/SmartComputerControl.desktop" << 'EOF'
[Desktop Entry]
Type=Application
Name=SmartComputerControl
Comment=Spegnimento remoto via Dropbox/IFTTT
Exec=__INSTALL_PATH__/SmartComputerControl
Icon=__INSTALL_PATH__/scc-icon.png
Terminal=false
Categories=Utility;
StartupNotify=false
EOF

# Copia icona PNG
if [ -f "Assets/scc-icon.png" ]; then
    cp "Assets/scc-icon.png" "$PUBLISH_DIR/"
fi

# 4. Script di installazione che l'utente eseguirà sul suo Linux
cat > "$PUBLISH_DIR/install.sh" << 'EOF'
#!/bin/bash
# Installer per SmartComputerControl su Linux
set -e

INSTALL_DIR="$HOME/.local/share/SmartComputerControl"
APPLICATIONS_DIR="$HOME/.local/share/applications"
AUTOSTART_DIR="$HOME/.config/autostart"

# Origine = cartella dove sta questo script
SRC_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "📦 Installo SmartComputerControl in $INSTALL_DIR..."

mkdir -p "$INSTALL_DIR"
cp -R "$SRC_DIR"/* "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/SmartComputerControl"

# Genera .desktop con path corretti
mkdir -p "$APPLICATIONS_DIR"
sed "s|__INSTALL_PATH__|$INSTALL_DIR|g" "$SRC_DIR/SmartComputerControl.desktop" > "$APPLICATIONS_DIR/SmartComputerControl.desktop"
chmod +x "$APPLICATIONS_DIR/SmartComputerControl.desktop"

# Update menu (gnome/kde)
if command -v update-desktop-database &> /dev/null; then
    update-desktop-database "$APPLICATIONS_DIR" 2>/dev/null || true
fi

# Suggerimento per shutdown senza password
echo ""
echo "✅ Installazione completata!"
echo ""
echo "Per usare SmartComputerControl: cerca 'SmartComputerControl' nel menu applicazioni"
echo ""
echo "⚠️  IMPORTANTE: per spegnere il PC senza password, esegui una volta:"
echo "   echo \"\$USER ALL=(ALL) NOPASSWD: /sbin/shutdown, /usr/sbin/shutdown\" | sudo tee /etc/sudoers.d/scc"
echo "   sudo chmod 440 /etc/sudoers.d/scc"
echo ""
echo "Per disinstallare: rm -rf $INSTALL_DIR e rimuovi $APPLICATIONS_DIR/SmartComputerControl.desktop"
EOF

chmod +x "$PUBLISH_DIR/install.sh"
chmod +x "$PUBLISH_DIR/SmartComputerControl"

# 5. README minimale
cat > "$PUBLISH_DIR/README.txt" << EOF
SmartComputerControl v$VERSION per Linux x64
==============================================

INSTALLAZIONE:
   Apri un terminale in questa cartella e lancia:
   ./install.sh

ESECUZIONE MANUALE (senza installare):
   ./SmartComputerControl

REQUISITI:
   - Ubuntu 22.04+ / Fedora 38+ / Arch (o equivalenti)
   - libnotify-bin (per le notifiche):
     sudo apt install libnotify-bin   (Ubuntu/Debian)
     sudo dnf install libnotify       (Fedora)

DISINSTALLAZIONE:
   rm -rf ~/.local/share/SmartComputerControl
   rm ~/.local/share/applications/SmartComputerControl.desktop
   rm ~/.config/autostart/SmartComputerControl.desktop
EOF

# 6. Tar.gz finale
cd "$DIST_DIR"
TARNAME="SmartComputerControl-${VERSION}-linux-x64.tar.gz"
tar -czf "$TARNAME" "$APP_NAME"
cd - > /dev/null

# Pulisce cartella temporanea
rm -rf "$PUBLISH_DIR"

echo ""
echo "============================================================"
echo "✅ Pacchetto Linux creato: $DIST_DIR/$TARNAME"
ls -lh "$DIST_DIR/$TARNAME"
echo "============================================================"
