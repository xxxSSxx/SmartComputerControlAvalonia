#!/bin/bash
# ============================================================
# update-and-run.sh
# Aggiorna il codice da git, compila il bundle .app e lo lancia.
# ============================================================

PROJ="$HOME/Documents/SmartComputerControlAvalonia"
APP="$PROJ/dist/SmartComputerControl.app"

cd "$PROJ" || { echo "❌ Cartella progetto non trovata: $PROJ"; read -n 1 -s; exit 1; }

echo "==> Aggiorno il codice (git pull)..."
git pull
echo ""

echo "==> Compilo il bundle .app per macOS..."
chmod +x build-mac-app.sh
if ! ./build-mac-app.sh; then
    echo ""
    echo ">>> Compilazione FALLITA. Premi un tasto per chiudere."
    read -n 1 -s
    exit 1
fi
echo ""

# Chiude eventuali istanze precedenti per evitare conflitti
pkill -f "SmartComputerControl.app/Contents/MacOS/SmartComputerControl" 2>/dev/null

echo "==> Avvio SmartComputerControl..."
open "$APP"
echo ""
echo "✅ Fatto."
