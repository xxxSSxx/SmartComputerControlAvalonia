# 📋 SmartComputerControl - Guida ai comandi

## Come funziona

L'app monitora una cartella (di solito una cartella Dropbox condivisa). Quando viene creato un file con un certo nome, l'app esegue un comando sul tuo computer.

### Regole base

- **Nome del file** = nome del computer di destinazione (es. `MacSe.txt` esegue il comando solo sul Mac chiamato "MacSe")
- **Contenuto del file** = comando da eseguire
- **File vuoto** = spegnimento (per retrocompatibilità con vecchie automazioni IFTTT)

### Esempi di uso pratico

Su un PC chiamato **WinSE** crei in Dropbox un file `WinSE.txt` contenente la parola `restart`:
→ Il PC Windows si riavvia.

Crei `MacSe.txt` con contenuto `volume:30`:
→ Il volume del Mac viene impostato al 30%.

Crei `UbuntuS.txt` con contenuto `notify:Sono arrivato a casa`:
→ Sul Linux appare una notifica con quel testo.

---

## 🔌 Comandi POWER (con conferma)

Questi comandi sono "distruttivi" (chiudono o riavviano il sistema). Per evitare incidenti, l'app mostra prima un **dialog di conferma con countdown di 30 secondi**. Puoi annullare cliccando "Annulla".

### `shutdown` (o file vuoto)
**Cosa fa:** Spegne completamente il computer.
**Quando usarlo:** Fine giornata, dimenticato di spegnere il PC dell'ufficio.
**Esempio file:** `WinSE.txt` con contenuto `shutdown` (oppure vuoto).

### `restart`
**Cosa fa:** Riavvia il computer.
**Quando usarlo:** Dopo un aggiornamento Windows, per pulire la RAM.
**Esempio file:** `WinSE.txt` con contenuto `restart`.

### `hibernate`
**Cosa fa:** Iberna il computer (salva lo stato su disco e spegne). Più lungo del `sleep`, ma il PC non consuma energia.
**Note:** Su Mac non esiste vera ibernazione, l'app esegue un `sleep` come fallback.
**Quando usarlo:** Vuoi spegnere risparmiando ma poter riprendere da dove avevi lasciato.
**Esempio file:** `WinSE.txt` con contenuto `hibernate`.

### `logoff` (alias: `logout`)
**Cosa fa:** Disconnette l'utente corrente. Le applicazioni vengono chiuse, ma il computer resta acceso.
**Quando usarlo:** Pulizia di sessione utente senza spegnere.
**Esempio file:** `WinSE.txt` con contenuto `logoff`.

---

## 🔌 Comandi POWER (immediati)

Questi sono **comandi sicuri** che si eseguono subito senza conferma, perché reversibili.

### `sleep`
**Cosa fa:** Sospende il computer (la RAM rimane alimentata, il PC consuma poco). Si risveglia premendo un tasto o muovendo il mouse.
**Quando usarlo:** Pausa breve, dieci minuti.
**Esempio file:** `WinSE.txt` con contenuto `sleep`.

### `lock`
**Cosa fa:** Blocca lo schermo. Per rientrare devi inserire la password.
**Note:** Su Mac simula `Cmd+Ctrl+Q` (richiede permessi "Accessibilità" la prima volta).
**Quando usarlo:** Vai a prendere un caffè, vuoi sicurezza.
**Esempio file:** `WinSE.txt` con contenuto `lock`.

### `screen-off` (alias: `screenoff`)
**Cosa fa:** Spegne solo il monitor (il PC resta acceso e lavora). Si riaccende muovendo il mouse o premendo un tasto.
**Quando usarlo:** Risparmio energia mentre il PC continua a fare un download/build.
**Esempio file:** `WinSE.txt` con contenuto `screen-off`.

---

## 🔊 Comandi AUDIO

### `mute`
**Cosa fa:** Silenzia l'audio del sistema (non cambia il volume, solo lo silenzia).
**Esempio file:** `MacSe.txt` con contenuto `mute`.

### `unmute`
**Cosa fa:** Riattiva l'audio del sistema (se era mutato).
**Esempio file:** `MacSe.txt` con contenuto `unmute`.

### `volume:N`
**Cosa fa:** Imposta il volume al N% (numero da 0 a 100).
**Esempio file:** `MacSe.txt` con contenuto `volume:50` → volume al 50%.
**Casi tipici:** `volume:0` (silenzio), `volume:100` (massimo), `volume:30` (basso).

### `volume-up` (alias: `volumeup`)
**Cosa fa:** Aumenta il volume del 10% rispetto al livello attuale.
**Esempio file:** `MacSe.txt` con contenuto `volume-up`.

### `volume-down` (alias: `volumedown`)
**Cosa fa:** Diminuisce il volume del 10% rispetto al livello attuale.
**Esempio file:** `MacSe.txt` con contenuto `volume-down`.

---

## 💬 Comandi SISTEMA

### `notify:testo libero`
**Cosa fa:** Mostra una notifica push sul computer con il testo che scrivi dopo i due punti.
**Quando usarlo:**
- Mandare un promemoria a un altro computer ("hai una riunione tra 10 minuti")
- Avvisare di un evento ("sono arrivato a casa")
- Sostituto di SMS tra dispositivi

**Esempio file:** `MacSe.txt` con contenuto:
```
notify:Chiamami quando puoi!
```
Sul Mac apparirà una notifica con titolo "Messaggio remoto" e corpo "Chiamami quando puoi!".

### `say:testo libero`
**Cosa fa:** Il computer pronuncia il testo ad alta voce con la sintesi vocale.
**Quando usarlo:**
- "Ricordare" qualcosa a chi sta vicino al PC (genitori, partner)
- Far parlare il PC dell'ufficio per scherzo (con moderazione 😄)
- Pronunciare notifiche audio per chi è dall'altra parte della stanza

**Esempio file:** `MacSe.txt` con contenuto:
```
say:Il forno è pronto, vai a controllarlo!
```
**Note Linux:** richiede `speech-dispatcher` (installato automaticamente dall'install.sh).

### `screenshot`
**Cosa fa:** Cattura tutto lo schermo e salva il file PNG sul Desktop con nome `screenshot_YYYYMMDD_HHMMSS.png`.
**Quando usarlo:** "Cosa sta facendo il PC adesso?" (genitori che vogliono controllare cosa fa il figlio).
**Esempio file:** `WinSE.txt` con contenuto `screenshot`.
**Note Linux:** richiede `gnome-screenshot` (installato automaticamente).

### `beep`
**Cosa fa:** Suona un beep di sistema (per richiamare attenzione).
**Esempio file:** `MacSe.txt` con contenuto `beep`.

### `eject`
**Cosa fa:** Espelle un CD/DVD/USB/disco esterno collegato.
**Quando usarlo:** Hai un disco esterno collegato che vuoi staccare in sicurezza senza alzarti.
**Esempio file:** `WinSE.txt` con contenuto `eject`.

---

## 🌐 Comandi RETE

### `wifi-on` (alias: `wifion`)
**Cosa fa:** Accende il WiFi del computer.

### `wifi-off` (alias: `wifioff`)
**Cosa fa:** Spegne il WiFi del computer.
**⚠️ ATTENZIONE:** Se hai connesso il computer in remoto via WiFi, dopo `wifi-off` non potrai più controllarlo!
**Esempio file:** `MacSe.txt` con contenuto `wifi-off`.

---

## 🚀 Comandi APP

### `open:nomeapp`
**Cosa fa:** Apre un'applicazione per nome.
**Note:**
- **Mac:** usa il nome come appare in Applicazioni (es. `Firefox`, `Spotify`, `Notes`)
- **Windows:** apre con `Start-Process`. Funziona con app installate (es. `notepad`, `firefox`, `chrome`)
- **Linux:** prova prima il nome diretto, poi `xdg-open` (es. `firefox`, `gnome-calculator`)

**Esempi:**
```
open:Firefox
open:Spotify
open:notepad
open:calculator
```

### `close:nomeapp` (alias: `quit:nomeapp`)
**Cosa fa:** Chiude un'applicazione.
**Esempi:**
```
close:Firefox
close:Spotify
close:Notepad
```

### `kill-all-browsers` (alias: `killallbrowsers`)
**Cosa fa:** Chiude TUTTI i browser aperti contemporaneamente: Chrome, Firefox, Edge, Brave, Opera, Safari.
**Quando usarlo:** Pulizia rapida quando il PC è lento per troppi tab aperti.
**Esempio file:** `WinSE.txt` con contenuto `kill-all-browsers`.

---

## 📝 Note tecniche

### Maiuscole/minuscole
Tutti i comandi sono **case-insensitive**: `SHUTDOWN`, `Shutdown`, `shutdown` funzionano allo stesso modo. **ECCETTO** per i parametri di `notify:`, `say:`, `open:`, `close:` che mantengono il case originale (perché magari serve scrivere "Ciao Mamma!" con maiuscole).

### File vuoto
Per retrocompatibilità, un file con nome `MioPC.txt` ma **completamente vuoto** equivale a `shutdown`. Era il vecchio formato pre-comandi multipli.

### Errori e fallback
Se scrivi un comando sconosciuto (es. `pippo`), l'app non fa nulla e logga "Comando sconosciuto" nei suoi log. Non blocca, non causa danni.

### Modalità Test (DebugMode)
Nelle impostazioni dell'app c'è una checkbox "🧪 Modalità test". Se attiva:
- I comandi vengono **simulati** (log + notifica con prefisso 🧪 [TEST])
- I comandi distruttivi (shutdown/restart) NON vengono eseguiti davvero
- Utile per testare nuove integrazioni IFTTT senza spegnere il PC per sbaglio

---

## 🎯 Esempi pratici di automazioni IFTTT

Combinando SmartComputerControl con IFTTT puoi creare automazioni vocali tramite Google Home, Alexa o Siri:

| Frase vocale | Azione IFTTT | Risultato |
|---|---|---|
| "Hey Google, spegni il PC dell'ufficio" | Crea file Dropbox `WinSE.txt` con contenuto `shutdown` | Il PC ufficio si spegne |
| "Alexa, alza il volume del Mac al 50%" | Crea `MacSe.txt` con contenuto `volume:50` | Volume Mac → 50% |
| "Hey Siri, manda un promemoria a Mac" | Crea `MacSe.txt` con contenuto `notify:Riunione fra 10 minuti` | Notifica push sul Mac |
| "Alexa, blocca tutti i PC" | Crea 3 file (uno per PC) con `lock` | Tutti i PC si bloccano |
| "Hey Google, fai uno screenshot del PC ufficio" | Crea `WinSE.txt` con contenuto `screenshot` | Screenshot sul Desktop del PC ufficio |

---

## 🆘 Risoluzione problemi

### Il comando viene ricevuto ma non eseguito
- Controlla che **DebugMode** sia disattivato nelle impostazioni
- Su Linux verifica le dipendenze: `which notify-send gnome-screenshot spd-say eject`
- Su Mac verifica i permessi: Impostazioni → Privacy → Accessibilità deve includere SmartComputerControl

### Le notifiche non appaiono (Linux)
- Verifica che `notify-send` funzioni: `notify-send "Test" "Funziona?"`
- Su Ubuntu 26.04+ Wayland alcuni nomi di app vengono filtrati: SmartComputerControl usa "Smart Computer Control" (con spazi) per aggirare il filtro

### Il file viene eliminato ma non succede niente
- Verifica che il **nome del file** corrisponda esattamente al nome del computer impostato nelle preferenze dell'app
- Case sensitive: `MacSe.txt` ≠ `macse.txt` ≠ `MACSE.txt`

### Sudoers su Linux per shutdown
Se lo shutdown su Linux richiede la password, lo script `install.sh` non ha configurato correttamente il file `/etc/sudoers.d/smartcomputercontrol`. Esegui manualmente:
```bash
echo "$USER ALL=(ALL) NOPASSWD: /sbin/shutdown, /usr/sbin/shutdown" | sudo tee /etc/sudoers.d/smartcomputercontrol
sudo chmod 440 /etc/sudoers.d/smartcomputercontrol
```
