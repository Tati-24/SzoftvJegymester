Csapat:
  Tóth Attila - Backend
  Farkas Levente - Backend
  Kelemen Ramóna - Frontend
  Molnárová Bernadett - Frontend


Jelenlegi backend helyzet:
  - Adatbázis felépítve
  - Szerver és adabtázis kezelés elérhető (MariaDb SQL, Docker szerverkezelés - localhost egyenlöre, PhpMyAdmin)
  - Regisztráció API működik - a kulcs kér:
                                            - Név: string
                                            - Email: string (beépített regexxel figyelt így csak valós email formátum lehet)
                                            - Jelszó (Legalább 8 karakter legyen)
                                            - Telefonszám (Csak telefonszám formátum engedett)
                                            - Sima regisztrációnál a 0 kódot kapja mint user "role" (0 = user, 1 = cashier, 2 = admin)
  - Regisztrált felhasználót email cím alapján ellenőrizzük, hogy létezik-e

  - Bejelentkezés(Login) API működik - a kulcs kér:
                                                    - Email: string (Ugyan az a funkció alapján kezeljük)
                                                    - Jelszó: ha helytelen akkor hibát ad vissza ezzel jelezve hogy hibás a jelszó

    

  - Filmek API: - Filmek megtekintése egyben és szűrve - ha nincs megadott értékhez film akkor nem jelenít meg semmit
                - Filmek törlése - ha nincs megadott film akkor nem lehet törölni, ha aktív akkor ki kell kapcsolni hogy törölhető legyen
                - Filmek módosítása - ha nincs film megadott értékek alapján akkor nem updatel, nem lehet kétszer ugyan az a film, 
                - Filmek feltöltése - ha van megadott film akkor nem lehet feltölteni

Mire lesz szükség még:
                      - Adatok módosítása
                      - Filmek feltöltése/kezelése (admin, user, cashier)
                      - Filmek és vetítések megtekintése
                      - Esetleges email rendszer ami visszaigazolja a vásárlást és imitálja a jegyet
                      - Guest vásárlás
                      - Cashier role, kontroller létrehozása és funkciói
                      - Admin role, kontroller létrehozás és funkciói

Jelenlegi frontend helyzet: ?
