# NillyHub — Nilly Suite για Windows 10/11

**[Δημόσια έκδοση v1.6 · Public release](https://github.com/nillynp/nillyhub/releases/tag/v1.6)**

Δωρεάν, πανάλαφρος launcher & updater για όλη τη Nilly Suite, με άδεια MIT (0 εξωτερικά DLLs, 0 MB RAM όταν είναι κλειστό).

- **Αυτόματη Ανακάλυψη Νέων Εργαλείων & Πραγματικά Εικονίδια (`catalog.json` + `.iconcache`):** κάθε νέο εργαλείο (π.χ. **NillyZip Pro**, **NillySearch Pro** και όσα ακολουθήσουν!) εμφανίζεται αυτόματα στο NillyHub με το πραγματικό του `.ico` εικονίδιο ακόμα και πριν εγκατασταθεί.
- **Αναλυτική Σύγκριση Πόρων (`ⓘ` σε κάθε κάρτα):** πάτα το `ⓘ` σε οποιαδήποτε εφαρμογή για να δεις αναλυτικό πίνακα σύγκρισης με τις default/βαριές εφαρμογές των Windows (**Χώρος Δίσκου · Ενεργή & Idle RAM · CPU & GPU φόρτος · Background Services · Ιδιωτικότητα**).
- Άνοιξε τα εργαλεία, επίλεξε μεμονωμένη εγκατάσταση ή χρησιμοποίησε το κουμπί «Όλη η Σουίτα» — ή δεξί κλικ σε κάθε κάρτα για άνοιγμα / ενημέρωση / φάκελο / σύγκριση / απεγκατάσταση.
- **Ενσωματωμένο Self-Update (SHA-256):** το ίδιο το NillyHub ελέγχει αυτόματα για νεότερη έκδοση χωρίς extra API calls και ενημερώνεται με 1 κλικ.
- **Αυτόματος Εντοπισμός Τοπικών Εργαλείων:** αναγνωρίζει εγκαταστάσεις στο `%LOCALAPPDATA%\NillySuite`, στο Windows Registry και από συντομεύσεις (Desktop & Start Menu) / διπλανούς φακέλους.
- **ETag Cache (`304 Not Modified`) & Άμεση Εκκίνηση:** φορτώνει άμεσα από τοπική cache χωρίς να σπαταλάει το GitHub API rate limit.
- **Επαληθευμένες Ενημερώσεις (SHA-256 + Αυτόματο Rollback):** ελέγχει μέγεθος & SHA-256 κάθε portable ZIP, κρατάει προσωρινό backup επαναφοράς σε περίπτωση σφάλματος και το καθαρίζει αυτόματα μόλις ολοκληρωθεί επιτυχώς η ενημέρωση.
- Το «⇄ Windows» ανοίγει τις Προεπιλεγμένες εφαρμογές ή τις ρυθμίσεις Προχείρου για χειροκίνητη επιλογή ιστορικού και συγχρονισμού.
- Συντομεύσεις: `Ctrl+F` για άμεση αναζήτηση, `Esc` για καθαρισμό φίλτρου.

Η μπάρα σύγκρισης δείχνει **99,98% πιο ελαφριά (`~1,35 MB` ZIP για όλη τη σουίτα vs `~6 GB` αντίστοιχων εφαρμογών Windows & Adobe)** (έκδοση 1.6). Τα μεγέθη μεταβάλλονται με τα releases.

## Εγκατάσταση σε ΟΠΟΙΟΔΗΠΟΤΕ Windows 10/11 PC (100% Portable · Χωρίς Admin)

1. Κατέβασε το **`NillyHub-v1.6-Portable.zip`** και αποσυμπίεσέ το όπου θες (π.χ. `C:\Tools\NillySuite\NillyHub`).
2. Τρέξε με διπλό κλικ το **`Install_NillyHub.bat`** *(δεν απαιτεί δικαιώματα Administrator!)*.
   - Μεταγλωττίζει αυτόματα το 100% native **`NillyHub.exe` (~62 KB)** με 0 εξωτερικά DLLs.
   - Φτιάχνει συντομεύσεις σε Επιφάνεια Εργασίας & Start Menu — και από μέσα εγκαθιστάς με 1 κλικ όποια από τα 11 δωρεάν εργαλεία θέλεις!

### Απεγκατάσταση
Τρέξε το **`Uninstall_NillyHub.bat`** — αφαιρεί μόνο τις συντομεύσεις του NillyHub χωρίς να αγγίξει τα εγκατεστημένα εργαλεία σου.

---

## Οικοσύστημα Δωρεάν Εργαλείων Windows — created by Nilly graphics (Nillygraphics · Sotiris Christos)

| Εργαλείο | Τι αντικαθιστά | Μέγεθος | Σύνδεσμος |
| :--- | :--- | :---: | :--- |
| **NillyClip Pro** | Clipboard history και κλειδοθήκη | ~62 KB | [GitHub](https://github.com/nillynp/nillyclip-pro) |
| **NillyHub** | All-in-One Nilly Suite Launcher & Auto-Updater | ~62 KB | [GitHub](https://github.com/nillynp/nillyhub) |
| **NillyDisk Pro** | TreeSize / WinDirStat / Duplicate Finder / Cache Cleaner | ~79 KB | [GitHub](https://github.com/nillynp/nillydisk-pro) |
| **NillyNote Pro** | Windows Notepad (18 MB) | ~78 KB | [GitHub](https://github.com/nillynp/nillynote-pro) |
| **NillyCode Pro** | Sublime Text (58 MB) | ~104 KB | [GitHub](https://github.com/nillynp/nillycode-pro) |
| **NillyCalc Pro** | Windows Calculator (18 MB) | ~72 KB | [GitHub](https://github.com/nillynp/nillycalc-pro) |
| **NillyBatch Pro** | Adobe Bridge (1.95 GB) | ~66 KB | [GitHub](https://github.com/nillynp/nillybatch-pro) |
| **NillyMedia Lite** | Windows Media Player & Voice Recorder (46 MB) | ~52 KB | [GitHub](https://github.com/nillynp/nillymedia-lite) |
| **NillyQuickSnap** | Windows Snipping Tool (586 MB) | ~49 KB | [GitHub](https://github.com/nillynp/nillyquicksnap) |
| **NillyView Pro** | Windows Photos & Paint | ~48 KB | [GitHub](https://github.com/nillynp/nillyview-pro) |
| **NillyDoc Pro** | Heavy PDF Viewers / Editors | ~2 MB | [GitHub](https://github.com/nillynp/nillydoc-pro) |
| **PC Manager & Tools** | All-in-One Studio & PC Maintenance Suite | Portable | [GitHub](https://github.com/nillynp/nilly-pc-manager-tools) |

---

## Author, Credits & SEO

- **Created by:** **[Nilly graphics (Nillygraphics · Sotiris Christos)](https://nillygraphics.com/)**
- **Website:** [https://nillygraphics.com/](https://nillygraphics.com/)
- **GitHub:** [https://github.com/nillynp](https://github.com/nillynp)
- **License:** MIT License — 100% Free & Open Source (0€)

> **English Summary:** **NillyHub** is a free, ultra-lightweight (~62 KB), open-source native Windows 10/11 app launcher and live GitHub auto-updater for the entire Nilly graphics Suite (1-click install, 1-click update, automatic discovery of new tools, 0 MB background RAM) — **created by Nilly graphics (Nillygraphics · Sotiris Christos)** ([nillygraphics.com](https://nillygraphics.com/)).
> **Keywords:** `NillyHub`, `Nilly Suite`, `Nilly graphics`, `Nillygraphics`, `Sotiris Christos`, `nillygraphics.com`, `free Windows 11 software suite`, `lightweight app launcher`, `GitHub auto updater`, `portable Windows utilities`, `Adobe Creative Cloud lightweight alternative`
