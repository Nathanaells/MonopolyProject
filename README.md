# MonopolyProject

> Proyek individu dari Formulatrix sebagai salah satu sistem penilaian kelulusan.

Aplikasi permainan Monopoly berbasis web dengan backend ASP.NET Core dan frontend React + TypeScript.

---

## Daftar Isi

- [Gambaran Umum](#gambaran-umum)
- [Teknologi](#teknologi)
- [Arsitektur Proyek](#arsitektur-proyek)
- [Struktur Folder](#struktur-folder)
- [Domain Model](#domain-model)
  - [Entitas](#entitas)
  - [Enum](#enum)
  - [Interface](#interface)
  - [DTO](#dto)
- [Backend – API Endpoints](#backend--api-endpoints)
- [Logika Permainan (Game.cs)](#logika-permainan-gamecs)
- [Factories](#factories)
- [Frontend](#frontend)
  - [Halaman](#halaman)
  - [Komponen](#komponen)
  - [Service Layer](#service-layer)
  - [Interfaces TypeScript](#interfaces-typescript)
- [Testing](#testing)
- [Cara Menjalankan](#cara-menjalankan)
- [Konfigurasi](#konfigurasi)

---

## Gambaran Umum

MonopolyProject adalah implementasi digital dari permainan papan klasik Monopoly. Pemain dapat:

- Membuat sesi permainan dengan 2–8 pemain.
- Memilih token (piece) masing-masing pemain.
- Melempar dadu secara bergantian.
- Membeli properti, membangun rumah/hotel.
- Menjual properti dan bangunan ke bank.
- Menarik kartu Chance dan Community Chest.
- Dipenjara, membayar pajak, dan bangkrut.
- Memenangkan permainan ketika semua lawan bangkrut.

---

## Teknologi

| Lapisan | Teknologi |
|---|---|
| Backend | ASP.NET Core 8, C# 12 |
| Database | SQLite (via Entity Framework Core) |
| Logging | Serilog (Console + File + JSON) |
| Autentikasi | JWT Bearer |
| Frontend | React 19, TypeScript, Vite |
| Styling | Tailwind CSS v4 |
| Routing | React Router v7 |
| Notifikasi | React Toastify |
| Testing | NUnit 3 |

---

## Arsitektur Proyek

```
MonopolyProject/
├── Backend/          # ASP.NET Core Web API
├── GameService.Tests/ # Unit tests (NUnit)
├── client/           # React + TypeScript SPA
└── MonopolyProject.sln
```

Komunikasi antara frontend dan backend menggunakan **REST API** over HTTP.  
State permainan dikelola sepenuhnya di **memori server** (satu game aktif secara statik pada `GameController._activeGame`).

---

## Struktur Folder

### Backend

```
Backend/
├── Controller/
│   ├── GameController.cs     # Semua endpoint permainan
│   └── UserController.cs     # (reserved – saat ini dinonaktifkan)
├── Domain/
│   ├── DTOs/                 # GameResultDTO, RollTurnResult, HandleTileResultDTO
│   ├── Entities/             # Game, Player, Board, Tile, Asset, Dice, ...
│   ├── Enums/                # TileType, PropertyCity, Color, PieceType, CardBehaviour, GamePhase, JailRollResult
│   ├── Interfaces/           # IGame, IPlayer, IBoard, ITile, IAsset, IDice, ...
│   └── ValueObjects/         # Point, (lainnya)
├── DTOs/
│   ├── ApiDTOs.cs            # UserRegisterDTO, UserLoginDTO
│   └── GameDTO.cs            # Request/Response DTO untuk semua endpoint
├── Factories/
│   ├── BoardFactory.cs       # Membuat papan 40 tile
│   ├── CardFactory.cs        # Kartu Chance & Community Chest
│   ├── DiceFactory.cs        # Dua dadu standar
│   ├── MoneyFactory.cs       # Uang awal
│   ├── PieceFactory.cs       # 8 token standar
│   └── PlayerFactory.cs      # List pemain dari nama
├── Helpers/
│   ├── GameHelper.cs         # Harga rumah per warna grup
│   └── GameStateMapper.cs    # Mengubah Game → GameStateResponse
├── Migrations/               # EF Core migrations (SQLite)
├── Repositories/
│   └── UserRepository.cs     # (reserved – belum digunakan aktif)
├── Services/
│   └── UserService.cs        # (reserved – belum digunakan aktif)
├── Program.cs                # Entry point, DI setup, middleware
└── appsettings.json
```

### Client (Frontend)

```
client/src/
├── Components/
│   ├── DiceAnimation.tsx         # Animasi visual dadu
│   ├── GameOverPanel.tsx         # Panel layar akhir permainan
│   ├── MonopolyPieces.tsx        # SVG token pemain
│   ├── PiecePicker.tsx           # UI memilih token sebelum mulai
│   ├── PlayerPropertiesPanel.tsx # Panel properti pemain
│   ├── Tiles.tsx                 # Render tiap tile papan
│   └── UsePieceAnimation.ts      # Hook animasi perpindahan token
├── Constant/
│   ├── ColorHex.ts       # Peta warna properti ke hex
│   ├── PieceComponents.ts # Map PieceType → komponen SVG
│   ├── PlayerAssets.ts   # Avatar & warna tiap pemain
│   ├── ToastUI.ts        # Helper ShowSuccess / ShowError
│   └── Url.ts            # baseURL backend
├── Interfaces/
│   └── Interface.ts      # Semua TypeScript interface
├── Views/
│   ├── BaseLayout.tsx    # Layout wrapper (ToastContainer)
│   ├── Game.tsx          # Halaman utama permainan
│   └── Home.tsx          # Halaman setup pemain & pemilihan token
├── hooks/
│   ├── buildBoard.ts     # Membangun struktur grid papan dari TileData[]
│   └── createEmptyBoard.ts # Grid kosong awal
├── services/
│   └── gameService.ts    # Semua panggilan REST API ke backend
├── App.tsx               # Routing utama
└── main.tsx              # Entry point React
```

---

## Domain Model

### Entitas

| Kelas | Deskripsi |
|---|---|
| `Game` | Inti logika permainan: state, turn, dadu, pemain, tile |
| `Player` | Data pemain: nama, status penjara, bangkrut, double roll |
| `Board` | Papan berisi array 40 `ITile` |
| `Tile` | Satu petak papan: jenis, posisi grid, aset, pemilik, rumah/hotel |
| `Asset` | Properti yang dapat dibeli: harga, kota, warna |
| `City` | Value object untuk `PropertyCity` |
| `Piece` | Token pemain di atas papan |
| `Dice` / `FakeDice` | Dadu random (1–6) atau dadu deterministik untuk testing |
| `Money` | Uang dengan nilai dari enum `MoneyValue` |
| `ChanceCard` | Kartu Chance: deskripsi + perilaku |
| `CommunityCard` | Kartu Community Chest: deskripsi + perilaku |

### Enum

#### `TileType` – Jenis petak
| Nilai | Keterangan |
|---|---|
| `StartTile` | Petak GO (awal) |
| `RentTile` | Properti yang dapat dibeli dan disewa |
| `RailroadTile` | Stasiun kereta api |
| `UtilityTile` | Perusahaan utilitas (listrik/air) |
| `JailTile` | Penjara (kunjungan biasa atau ditahan) |
| `GoToJailTile` | Petak "Go to Jail" |
| `DrawChance` | Ambil kartu Chance |
| `DrawCommunity` | Ambil kartu Community Chest |
| `TaxTile` | Pajak ringan |
| `PayTaxTile` | Pajak berat |
| `FreeParkingTile` | Free Parking (tidak ada efek) |
| `ActionTile` | Petak aksi generik |

#### `GamePhase` – Fase giliran
| Nilai | Keterangan |
|---|---|
| `WaitingRoll` | Menunggu pemain melempar dadu |
| `WaitingBuyDecision` | Menunggu keputusan beli/tidak |
| `TurnEnded` | Giliran selesai |

#### `PieceType` – Token tersedia
`Tophat`, `Car`, `ScottieDog`, `Battleship`, `Horse`, `Thimble`, `Cannon`, `Wheelbarrow`

#### `Color` – Grup warna properti
`Brown`, `LightBlue`, `Pink`, `Orange`, `Red`, `Yellow`, `Green`, `DarkBlue`

#### `JailRollResult`
`None` | `Released` | `StayedInJail`

#### `CardBehaviour` – Efek kartu (32 perilaku)
Contoh: `AdvanceToGo`, `GoToJail`, `GetOutOfJailFree`, `BankPaysDividend`, `Birthday`, dll.

### Interface

| Interface | Diimplementasi oleh |
|---|---|
| `IPlayer` | `Player` |
| `IBoard` | `Board` |
| `ITile` | `Tile` |
| `IAsset` | `Asset` |
| `ICity` | `City` |
| `IPiece` | `Piece` |
| `IDice` | `Dice`, `FakeDice` |
| `IMoney` | `Money` |
| `ICard` | `ChanceCard`, `CommunityCard` |
| `IUser` | `User` |

### DTO

#### Request DTO
| DTO | Field | Keterangan |
|---|---|---|
| `StartGameRequestDTO` | `PlayerNames: List<string>` | Daftar nama pemain (min 2) |
| `SelectPieceRequestDTO` | `PlayerName, PieceType` | Pilih token untuk pemain |
| `BuyPropertyRequestDTO` | `Buy: bool` | Konfirmasi beli properti |
| `BuyBuildingRequestDTO` | `PlayerName, City, BuildHotel` | Beli rumah atau hotel |
| `SellPropertyRequestDTO` | `PlayerName, City, IncludeBuildings` | Jual properti ke bank |
| `SellBuildingRequestDTO` | `PlayerName, City, HousesToSell, SellHotel` | Jual bangunan saja |
| `SellAllAssetsRequestDTO` | `PlayerName` | Jual semua aset pemain |
| `ExecuteCardRequestDTO` | `Behaviour, CardType, Description` | Eksekusi kartu yang ditarik |

#### Response DTO
| DTO | Field Utama |
|---|---|
| `GameStateResponse` | `IsGameEnded, Winner, CurrentPlayer, Players[]` |
| `PlayerResponseDTO` | `Name, Balance, IsInJail, IsBankrupt, Properties[], CurrentTileIndex` |
| `TileResponseDTO` | `Index, Type, Position, Asset, Owner, Houses, HasHotel` |
| `RollTurnResponseDTO` | `DiceTotal, Dice1, Dice2, LandedTileType, RequiresBuyDecision, DrawnCardDescription, JailRollResult, State` |
| `SellResultResponseDTO` | `Income, State` |
| `PieceResponseDTO` | `PieceType, IsAvailable` |

---

## Backend – API Endpoints

Base URL: `/api/game`

### POST `/start`
Memulai permainan baru.

**Request Body:**
```json
{ "playerNames": ["Alice", "Bob"] }
```

**Response:** `GameStateResponse`

**Validasi:** Minimal 2 pemain, nama harus unik.

---

### GET `/state`
Mengambil state permainan saat ini.

**Response:** `GameStateResponse`

---

### GET `/board/tiles`
Mengambil semua 40 tile papan beserta data aset, pemilik, dan bangunan.

**Response:** `List<TileResponseDTO>`

---

### GET `/pieces`
Mengambil daftar semua token dan ketersediaannya.

**Response:** `List<PieceResponseDTO>`

---

### POST `/select-piece`
Memilih token untuk seorang pemain.

**Request Body:**
```json
{ "playerName": "Alice", "pieceType": "Car" }
```

**Response:** `GameStateResponse`

---

### POST `/turn/roll`
Melempar dadu untuk pemain yang sedang giliran.

**Response:** `RollTurnResponseDTO`  
Jika mendarat di properti kosong dan pemain mampu membeli, `requiresBuyDecision` = `true`.

---

### POST `/turn/buy-property`
Konfirmasi keputusan beli atau lewati properti.

**Request Body:**
```json
{ "buy": true }
```

**Response:** `GameStateResponse`

---

### GET `/player-properties?playerName={name}`
Mendapatkan daftar properti milik pemain tertentu.

**Response:** `List<TileResponseDTO>`

---

### POST `/buy-building`
Membeli rumah atau hotel di properti yang dimiliki pemain.

**Request Body:**
```json
{ "playerName": "Alice", "city": "ParkPlace", "buildHotel": false }
```

**Response:** `GameStateResponse`

---

### POST `/sell-property`
Menjual properti beserta bangunannya ke bank.

**Request Body:**
```json
{ "playerName": "Alice", "city": "ParkPlace", "includeBuildings": true }
```

**Response:** `SellResultResponseDTO`

---

### POST `/sell-buildings`
Menjual sejumlah rumah atau hotel saja (tanpa menjual tanah).

**Request Body:**
```json
{ "playerName": "Alice", "city": "ParkPlace", "housesToSell": 2, "sellHotel": false }
```

**Response:** `SellResultResponseDTO`

---

### POST `/sell-all-assets`
Menjual semua properti dan bangunan milik satu pemain ke bank.

**Request Body:**
```json
{ "playerName": "Alice" }
```

**Response:** `SellResultResponseDTO`

---

### POST `/execute-card`
Mengeksekusi efek kartu Chance atau Community Chest yang sudah ditarik.

**Request Body:**
```json
{ "cardType": "Chance", "description": "Go to Jail", "behaviour": 8 }
```

**Response:** `GameStateResponse`

---

## Logika Permainan (Game.cs)

### Alur Giliran (`RollTurn`)

```
RollTurn()
 ├── Cek fase = WaitingRoll
 ├── Roll 2 dadu → total, isDouble
 ├── Jika pemain di penjara:
 │    ├── isDouble → keluar, move, HandleTile
 │    └── bukan double → kurangi JailTurnsRemaining
 │         └── habis → keluar paksa, move, HandleTile
 ├── Cek kebangkrutan (CheckBankruptcy)
 ├── Hitung DoubleRoll; jika >= 3 → kirim ke penjara
 ├── MovePiece(total) → pindah tile, lewati GO = +$200
 ├── HandleTileEffectsAfterMove(tile)
 │    ├── Properti kosong & mampu beli → RequiresBuyDecision = true
 │    └── Lainnya → ExecuteTile (sewa, pajak, jail, kartu, dll.)
 └── EndTurn(repeatTurn = isDouble)
      ├── EndGame() → cek pemenang
      └── jika bukan double → NextPlayer()
```

### Pergerakan Piece

- `MovePiece(player, steps)` – hitung index baru secara modular (40 tile); melewati index 0 memberikan $200.
- `MovePieceTo(player, targetTile)` – pindah langsung ke tile tertentu (digunakan efek kartu).
- `MoveToNearestUtility` / `MoveToNearestRailroad` – cari tile terdekat sesuai jenis.

### Penjara

- Pemain dikirim ke penjara jika: mendarat di `GoToJailTile`, efek kartu `GoToJail`, atau melempar double 3× berturut-turut.
- Di penjara, pemain memiliki 3 giliran untuk keluar (roll double, pakai kartu bebas penjara, atau bayar denda setelah giliran ke-3 habis).

### Pembangunan

- Harga rumah per warna: Brown/LightBlue = $50, Pink/Orange = $100, Red/Yellow = $150, Green/DarkBlue = $200.
- Pemain harus memiliki **semua properti dalam satu grup warna** sebelum membangun.
- Maksimum 4 rumah lalu 1 hotel per properti.

### Kebangkrutan & Akhir Permainan

- Pemain dinyatakan bangkrut jika saldo ≤ 0 dan tidak dapat memenuhi kewajiban.
- Permainan berakhir ketika hanya tersisa 1 pemain yang tidak bangkrut.

---

## Factories

| Factory | Kegunaan |
|---|---|
| `BoardFactory` | Membuat papan 40 tile sesuai tata letak Monopoly standar |
| `CardFactory` | Membuat 15 kartu Chance + 16 kartu Community Chest |
| `DiceFactory` | Membuat 2 objek `Dice` |
| `MoneyFactory` | Membuat pool uang permainan |
| `PieceFactory` | Membuat 8 token standar |
| `PlayerFactory` | Membuat `Player` dari array nama |

### Papan (40 Tile)

| Index | Jenis | Properti |
|---|---|---|
| 0 | StartTile | GO |
| 1 | RentTile | Mediterranean Avenue (Brown, $50) |
| 2 | DrawCommunity | – |
| 3 | RentTile | Baltic Avenue (Brown, $50) |
| 4 | TaxTile | – |
| 5 | RailroadTile | Reading Railroad ($100) |
| 6–9 | RentTile | Oriental, Vermont, Connecticut Ave (LightBlue) |
| 10 | JailTile | Jail / Just Visiting |
| 11–14 | RentTile | St. Charles, States, Virginia (Pink) |
| 12 | UtilityTile | Electric Company ($150) |
| 15 | RailroadTile | Pennsylvania Railroad ($200) |
| 16–19 | RentTile | St. James, Tennessee, New York (Orange) |
| 20 | FreeParkingTile | Free Parking |
| 21–24 | RentTile | Kentucky, Indiana, Illinois (Red) |
| 25 | RailroadTile | B&O Railroad ($350) |
| 26–29 | RentTile | Atlantic, Ventnor, Marvin Gardens (Yellow) |
| 28 | UtilityTile | Water Works ($150) |
| 30 | GoToJailTile | Go to Jail |
| 31–34 | RentTile | Pacific, N. Carolina, Pennsylvania Ave (Green) |
| 35 | RailroadTile | Short Line Railroad ($450) |
| 37 | RentTile | Park Place (DarkBlue, $350) |
| 38 | TaxTile | – |
| 39 | RentTile | Boardwalk (DarkBlue, $400) |

---

## Frontend

### Halaman

#### `Home.tsx`
Halaman setup sebelum permainan dimulai, dengan dua langkah (step):

1. **addPlayers** – Tambah 2–8 pemain (input nama + tombol Add / Enter). Klik "Choose Tokens →" untuk lanjut (memanggil `gameService.startGame`).
2. **pickPieces** – Setiap pemain memilih token melalui komponen `PiecePicker`. Setelah semua memilih, navigasi ke `/game`.

#### `Game.tsx`
Halaman utama permainan:
- Render papan Monopoly 11×11 menggunakan CSS Grid.
- Tampilkan token pemain di atas tile yang sesuai.
- Panel kontrol: tombol Roll, beli properti, melihat properti pemain.
- Panel sisi: status pemain (saldo, properti, status jail).
- Animasi dadu (`DiceAnimation`) dan perpindahan token (`UsePieceAnimation`).
- Panel akhir game (`GameOverPanel`) ketika permainan selesai.

#### `BaseLayout.tsx`
Layout wrapper minimal yang menyertakan `ToastContainer` untuk notifikasi global.

### Komponen

| Komponen | Fungsi |
|---|---|
| `Tiles.tsx` | Render visual satu tile papan (nama, warna, harga, token di atasnya) |
| `MonopolyPieces.tsx` | SVG inline tiap token (Tophat, Car, ScottieDog, dll.) |
| `PiecePicker.tsx` | Antarmuka memilih token sebelum game dimulai |
| `DiceAnimation.tsx` | Animasi visual dua dadu berputar |
| `GameOverPanel.tsx` | Modal/panel "Game Over" dengan nama pemenang |
| `PlayerPropertiesPanel.tsx` | Panel daftar properti + aksi jual/beli bangunan |
| `UsePieceAnimation.ts` | Custom hook yang menganimasikan perpindahan token secara bertahap tile demi tile |

### Service Layer

`gameService.ts` adalah objek singleton berisi semua panggilan API:

| Method | HTTP | Endpoint |
|---|---|---|
| `startGame(names)` | POST | `/start` |
| `getGameState()` | GET | `/state` |
| `getBoardTiles()` | GET | `/board/tiles` |
| `getAvailablePieces()` | GET | `/pieces` |
| `selectPiece(name, type)` | POST | `/select-piece` |
| `rollTurn()` | POST | `/turn/roll` |
| `buyProperty(buy)` | POST | `/turn/buy-property` |
| `getPlayerProperties(name)` | GET | `/player-properties` |
| `buyBuilding(name, city, hotel)` | POST | `/buy-building` |
| `sellProperty(name, city, incl)` | POST | `/sell-property` |
| `sellBuildings(name, city, n, hotel)` | POST | `/sell-buildings` |
| `sellAllAssets(name)` | POST | `/sell-all-assets` |
| `executeCard(type, desc, behaviour)` | POST | `/execute-card` |

### Interfaces TypeScript

Didefinisikan di `src/Interfaces/Interface.ts`:

```ts
GameState        // Status lengkap permainan
PlayerState      // Status satu pemain
TileData         // Data satu tile papan
RollResult       // Hasil lemparan dadu
PieceData        // Token + ketersediaan
SellResult       // Hasil penjualan aset
PendingMove      // Data animasi perpindahan tertunda
```

---

## Testing

Unit test berada di proyek `GameService.Tests` menggunakan **NUnit 3**.

### Setup

Setiap test menggunakan `FakeDice(6)` (dadu deterministik) sehingga hasil roll dapat diprediksi.

```csharp
[SetUp]
public void Setup()
{
    IBoard board = BoardFactory.CreateBoard();
    List<IPlayer> players = PlayerFactory.CreatePlayers(["Player1","Player2","Player3","Player4"]);
    List<IDice> dice = new List<IDice> { new FakeDice(6), new FakeDice(6) };
    _game = new Game(board, players, pieces, cards, money, dice);
}
```

### Cakupan Test (contoh)

- `IsPieceAvailable_PieceNotAssigned_ShouldReturnTrue`
- `IsPieceAvailable_PieceAssigned_ShouldReturnFalse`
- `AssignPieceToPlayer_ValidPlayerAndPiece_ShouldReturnSuccess`
- *(dan banyak lainnya untuk RollTurn, MovePiece, Jail, Sell, dll.)*

### Menjalankan Test

```bash
cd GameService.Tests
dotnet test
```

---

## Cara Menjalankan

### Prasyarat

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)

### Backend

```bash
cd Backend

# Salin file environment (isi JWT_KEY minimal 32 karakter)
cp appsettings.Development.json appsettings.Development.json

# Jalankan migrasi (jika database belum ada)
dotnet ef database update

# Jalankan server
dotnet run
```

Server berjalan di `https://localhost:5001` (atau port di `launchSettings.json`).  
Swagger UI tersedia di `https://localhost:5001/swagger` pada mode Development.

### Frontend

```bash
cd client
npm install
npm run dev
```

Aplikasi berjalan di `http://localhost:5173` secara default.

### Konfigurasi URL Backend di Frontend

Edit `client/src/Constant/Url.ts`:

```ts
export const baseURL = "https://localhost:5001/api/game";
```

---

## Konfigurasi

### `appsettings.json`

```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*"
}
```

### Environment Variables

| Variabel | Keterangan |
|---|---|
| `JWT_KEY` | Secret key untuk signing JWT token (min. 32 karakter) |

Buat file `.env` di folder `Backend/`:

```
JWT_KEY=your_super_secret_key_at_least_32_chars
```

### Logging

Log ditulis ke tiga target sekaligus:

1. **Console** – format human-readable dengan timestamp.
2. **File teks** – `logs/application-{tanggal}.log` (rolling harian).
3. **File JSON** – `logs/application-json-{tanggal}.json` (untuk analitik/monitoring).

---

## Lisensi

Lihat file [LICENSE](LICENSE) untuk informasi lisensi.
