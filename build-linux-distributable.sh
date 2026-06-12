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

echo "============================================================"
echo "        SmartComputerControl - Installer Linux"
echo "============================================================"
echo ""

# ===== 1. Rilevamento package manager =====
PKG_MGR=""
if command -v apt-get &> /dev/null; then
    PKG_MGR="apt"
elif command -v dnf &> /dev/null; then
    PKG_MGR="dnf"
elif command -v pacman &> /dev/null; then
    PKG_MGR="pacman"
fi

# ===== 2. Verifica dipendenze =====
echo "==> Verifico le dipendenze di sistema..."
MISSING=""

# notify-send (libnotify-bin)
if ! command -v notify-send &> /dev/null; then
    MISSING="$MISSING libnotify-bin"
fi

# Tray icon: su GNOME serve l'estensione appindicator
if [ -n "$XDG_CURRENT_DESKTOP" ] && echo "$XDG_CURRENT_DESKTOP" | grep -qi "gnome"; then
    # Controllo se l'estensione e' installata
    if ! gnome-extensions list 2>/dev/null | grep -q "appindicator"; then
        if [ "$PKG_MGR" = "apt" ]; then
            MISSING="$MISSING gnome-shell-extension-appindicator"
        elif [ "$PKG_MGR" = "dnf" ]; then
            MISSING="$MISSING gnome-shell-extension-appindicator"
        fi
    fi
fi

if [ -n "$MISSING" ]; then
    echo ""
    echo "Dipendenze mancanti:$MISSING"
    echo ""
    case "$PKG_MGR" in
        apt)
            echo "Le installo? (potrebbe chiedere la password sudo)"
            echo -n "[s/n]: "
            read RISP
            if [ "$RISP" = "s" ] || [ "$RISP" = "S" ]; then
                sudo apt update
                sudo apt install -y $MISSING
                echo ""
                echo "⚠️ Se hai installato l'estensione appindicator: fai logout/login"
                echo "   per attivarla, altrimenti la tray icon non sara' visibile."
            fi
            ;;
        dnf)
            echo "Le installo? (potrebbe chiedere la password sudo)"
            echo -n "[s/n]: "
            read RISP
            if [ "$RISP" = "s" ] || [ "$RISP" = "S" ]; then
                sudo dnf install -y $MISSING
            fi
            ;;
        *)
            echo "Installa manualmente:$MISSING"
            ;;
    esac
fi

# ===== 3. Installazione app =====
echo ""
echo "==> Installo SmartComputerControl in $INSTALL_DIR..."

mkdir -p "$INSTALL_DIR"
cp -R "$SRC_DIR"/* "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/SmartComputerControl"

# .desktop con path corretti
mkdir -p "$APPLICATIONS_DIR"
sed "s|__INSTALL_PATH__|$INSTALL_DIR|g" "$SRC_DIR/SmartComputerControl.desktop" > "$APPLICATIONS_DIR/SmartComputerControl.desktop"
chmod +x "$APPLICATIONS_DIR/SmartComputerControl.desktop"

# Update menu (gnome/kde)
if command -v update-desktop-database &> /dev/null; then
    update-desktop-database "$APPLICATIONS_DIR" 2>/dev/null || true
fi

# ===== 4. Regola sudoers per spegnimento senza password =====
echo ""
echo "==> Configurazione spegnimento senza password..."
SUDOERS_FILE="/etc/sudoers.d/smartcomputercontrol"
if [ ! -f "$SUDOERS_FILE" ]; then
    echo "Per spegnere il PC senza richiedere la password, serve creare:"
    echo "  $SUDOERS_FILE"
    echo ""
    echo "Lo configuro? (richiede password sudo)"
    echo -n "[s/n]: "
    read RISP
    if [ "$RISP" = "s" ] || [ "$RISP" = "S" ]; then
        echo "$USER ALL=(ALL) NOPASSWD: /sbin/shutdown, /usr/sbin/shutdown" | sudo tee "$SUDOERS_FILE" > /dev/null
        sudo chmod 440 "$SUDOERS_FILE"
        echo "✅ Configurato."
    else
        echo "⚠️ Senza questa regola, il sistema chiederà la password quando ricevi un comando di spegnimento."
    fi
fi

# ===== 5. Fine =====
echo ""
echo "============================================================"
echo "✅ Installazione completata!"
echo "============================================================"
echo ""
echo "Per avviare: cerca 'SmartComputerControl' nel menu applicazioni."
echo ""
echo "Per disinstallare:"
echo "   rm -rf $INSTALL_DIR"
echo "   rm $APPLICATIONS_DIR/SmartComputerControl.desktop"
echo "   rm $AUTOSTART_DIR/SmartComputerControl.desktop  # se configurato"
echo "   sudo rm $SUDOERS_FILE  # opzionale"
echo ""
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
