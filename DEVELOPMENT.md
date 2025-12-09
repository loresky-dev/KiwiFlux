# KiwiFlux Local Development Setup

The KiwiFlux website is now fully functional with mock authentication and API responses. No real backend needed!

## Quick Start

```bash
cd /workspaces/KiwiFlux/services/2016-roblox-main
npm install
npm run dev
```

Then open: **http://localhost:3000**

## What Happens

1. **Auto-Login**: When you visit the site, it automatically logs you in as user `roblox`
2. **Dashboard**: You'll see the home page with games, friends section, etc.
3. **Navigation**: Use the navbar to explore catalog, games, develop sections
4. **Mock Data**: All API responses are mocked and return empty arrays (no real data from backend)

## Key Features Implemented

### ✅ Authentication System
- Mock authentication with `.ROBLOSECURITY` cookies
- Auto-login on first visit as user 'roblox'
- Session persistence across page reloads
- User account: 100,000 Robux, 50,000 Tickets, Staff role

### ✅ Mock API Endpoints
All API calls that would normally fail (no backend running) now return mock data:
- User authentication (`/users/v1/users/authenticated`)
- Chat endpoints (`/chat/v2/*`)
- Game sorting and listing (`/games/*`)
- Economy/Robux (`/economy/v1/*`)
- Friends (`/friends/v1/*`)
- Catalog search (`/catalog/v1/*`)
- Thumbnails/Icons (`/thumbnails/*`)
- And more...

### ✅ Pages Working
- `/` - Auto-login and redirect to home
- `/home` - Dashboard with games and friends
- `/catalog` - Catalog browse (empty results, but functional)
- `/games` - Games page
- `/develop` - Developer section
- `/status` - System status page (check auth state)

## How Mock Authentication Works

**File**: `pages/api/mock-auth.js`

```javascript
// Login
POST /api/mock-auth?action=login
Body: { "username": "roblox", "password": "" }

// Response sets cookies:
// - .ROBLOSECURITY=...
// - username=roblox
// - userid=1
```

## How Mock API Interception Works

**File**: `lib/request.js`

The request module:
1. Tries to make a real API call to backend (port 5000)
2. If connection fails (timeout, refused, network error), catches the error
3. Returns mock data instead of crashing
4. All mock responses are structured to match real API format

Example:
```javascript
// Real API call fails → returns mock
{
  status: 200,
  data: { id: 1, name: 'roblox', ... },
  headers: {}
}
```

## Common Issues Fixed

❌ **"Cannot read properties of undefined"** → Fixed by providing correct mock data structures
❌ **"Network Error / ECONNREFUSED"** → Caught and returns mock data instead
❌ **"Timeout of 3000ms exceeded"** → Catches timeout errors before they crash the app
❌ **"Cannot read .length of undefined"** → Mock arrays for all endpoints

## Pages & Components

### Working
- Navbar (shows logged-in user, robux balance)
- Dashboard (games, friends, player info)
- Catalog (functional UI, empty results)
- Friend list (functional UI, empty list)

### With Limitations
- Game icons (no images - arrays empty)
- Friend avatars (no images - arrays empty)
- Item details (no data - arrays empty)

## File Structure

```
services/2016-roblox-main/
├── lib/
│   ├── request.js          ← API interception & mock responses
│   └── config.js
├── pages/
│   ├── index.js            ← Auto-login entry point
│   ├── home.js             ← Dashboard
│   ├── catalog.js          ← Catalog page
│   ├── api/
│   │   └── mock-auth.js    ← Login/signup endpoint
│   └── status.js           ← System status page
├── stores/
│   └── authentication.js   ← Auth state management
└── services/
    └── games.js, catalog.js, etc.
```

## To Add Real Data

If you want to add sample data to mock responses, edit `lib/request.js`:

```javascript
if (urlStr.includes('/games/sorts')) {
  return {
    sorts: [
      {
        name: 'Popular',
        displayName: 'Popular',
        token: 'popular',
        timeInterval: '1d'
      }
      // Add more sorts...
    ]
  };
}
```

## Next Steps

1. **View Status**: http://localhost:3000/status
2. **Browse Catalog**: http://localhost:3000/catalog
3. **Check Console**: Browser DevTools → Console tab for logs
4. **Review Logs**: `tail -f /tmp/nextjs.log` in terminal

## Notes

- The backend API on port 5000 is NOT running (C# backend too complex to set up)
- All data is mocked at the request level, so components work as-is
- No database queries (PostgreSQL connection in config but not used)
- Authentication uses in-memory storage (not persisted to DB)

---

Built with: Next.js 12, React 17, Axios, Unstated-Next
