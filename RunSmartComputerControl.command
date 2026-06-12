#!/bin/bash
# ============================================================
# RunSmartComputerControl.command
# Menu unico per le operazioni Mac.
# Funziona sia se messo nel progetto sia sul Desktop.
# ============================================================

# Cartella dove si trova questo .command
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Se il .command e' dentro la cartella del progetto, usa quella;
# altrimenti (es. sul Desktop) cerca in ~/Documents/
if [ -f "$SCRIPT_DIR/SmartComputerControlAvalonia.csproj" ]; then
    PROJ="$SCRIPT_DIR"
else
    PROJ="$HOME/Documents/SmartComputerControlAvalonia"
fi

APP_NAME="SmartComputerControl"
INSTALLED_APP="/Applications/$APP_NAME.app"

if [ ! -d "$PROJ" ]; then
    echo "❌ Cartella progetto non trovata: $PROJ"
    read -n 1 -s
    exit 1
fi

cd "$PROJ"

clear
echo "============================================================"
echo "         SmartComputerControl - Menu di gestione"
echo "============================================================"
echo ""
echo "  1) Aggiorna e avvia l'app (uso normale)"
echo "  2) Crea pacchetti distribuibili Mac (.dmg arm64 + intel)"
echo "  3) Crea pacchetto distribuibile Linux (.tar.gz)"
echo "  4) Commit & Push su GitHub"
echo "  5) Esci"
echo ""
echo -n "Scelta [1-5]: "
read SCELTA

case "$SCELTA" in

    1)
        echo ""
        echo "==> Chiudo l'app se in esecuzione..."
        killall "$APP_NAME" 2>/dev/null || true

        echo "==> Rimuovo vecchia installazione..."
        rm -rf "$INSTALLED_APP"

        echo "==> Aggiorno il codice (git pull)..."
        git reset --hard HEAD
        git pull

        echo "==> Compilo il bundle .app..."
        chmod +x build-mac-app.sh
        if ! ./build-mac-app.sh; then
            echo ">>> Compilazione FALLITA."
            read -n 1 -s -p "Premi un tasto per chiudere..."
            exit 1
        fi

        echo "==> Installo in /Applications..."
        cp -R "$PROJ/dist/$APP_NAME.app" "$INSTALLED_APP"

        LSREGISTER="/System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/LaunchServices.framework/Versions/A/Support/lsregister"
        if [ -x "$LSREGISTER" ]; then
            "$LSREGISTER" -f "$INSTALLED_APP" 2>/dev/null || true
        fi

        echo "==> Avvio SmartComputerControl..."
        open "$INSTALLED_APP"

        echo ""
        echo "✅ Fatto."
        ;;

    2)
        echo ""
        echo "==> Aggiorno il codice (git pull)..."
        git reset --hard HEAD
        git pull

        echo "==> Compilo i pacchetti .dmg per Mac (arm64 + intel)..."
        chmod +x build-mac-distributable.sh build-mac-app.sh
        if ! ./build-mac-distributable.sh; then
            echo ">>> Compilazione FALLITA."
            read -n 1 -s -p "Premi un tasto per chiudere..."
            exit 1
        fi

        echo ""
        echo "✅ Pacchetti creati in $PROJ/dist-mac/"
        echo "Apro la cartella nel Finder..."
        open "$PROJ/dist-mac"
        ;;

    3)
        echo ""
        echo "==> Aggiorno il codice (git pull)..."
        git reset --hard HEAD
        git pull

        echo "==> Compilo il pacchetto Linux x64..."
        chmod +x build-linux-distributable.sh
        if ! ./build-linux-distributable.sh; then
            echo ">>> Compilazione FALLITA."
            read -n 1 -s -p "Premi un tasto per chiudere..."
            exit 1
        fi

        echo ""
        echo "✅ Pacchetto Linux creato in $PROJ/dist-linux/"
        echo "Apro la cartella nel Finder..."
        open "$PROJ/dist-linux"
        ;;

    4)
        echo ""
        echo "==> Stato del repository:"
        git status -s
        echo ""
        if git status | grep -q "nothing to commit"; then
            echo "Nessuna modifica da committare."
        else
            echo ""
            echo -n "Messaggio di commit: "
            read MSG
            if [ -z "$MSG" ]; then
                echo "Messaggio vuoto, annullo."
            else
                echo ""
                echo "==> git add ..."
                git add .
                echo "==> git commit ..."
                git commit -m "$MSG"
                echo "==> git push ..."
                if git push; then
                    echo ""
                    echo "✅ Push completato!"
                else
                    echo ""
                    echo ">>> Push FALLITO. Controlla l'output sopra."
                fi
            fi
        fi
        ;;

    5)
        exit 0
        ;;

    *)
        echo "Scelta non valida."
        ;;
esac

echo ""
read -n 1 -s -p "Premi un tasto per chiudere..."
