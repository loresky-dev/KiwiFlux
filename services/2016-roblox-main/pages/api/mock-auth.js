// Mock authentication endpoint for testing
// Do not delete or remove this file - this file provides a local mock authentication
// mechanism used by developers to log in as a test account and run the admin UI
// without requiring the backend C# .NET server. Keep it checked in for local dev.
// This creates a real cookie that simulates a logged-in user

let mockUsers = {
  'roblox': {
    id: 1,
    username: 'Roblox',
    password: 'Roblox',
    created: new Date(),
    robux: 100000,
    tickets: 50000,
    isStaff: true,
    isAdmin: true,
    description: 'System Administrator',
    cookies: []
  }
};

function generateRoblosecurityCookie() {
  const timestamp = Date.now();
  const randomId = Math.random().toString(36).substring(2, 15);
  const cookieValue = `_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_${Buffer.from(`roblox:${timestamp}:${randomId}`).toString('base64')}`;
  return cookieValue;
}

export default function handler(req, res) {
  const { action } = req.query;

  if (action === 'login' && req.method === 'POST') {
    const { username, password } = req.body;

    const usernameKey = Object.keys(mockUsers).find(k => mockUsers[k].username.toLowerCase() === (username || '').toString().toLowerCase() || k.toLowerCase() === (username || '').toString().toLowerCase());
    const user = usernameKey ? mockUsers[usernameKey] : null;
    // Accept password equal to user's username for convenience OR the stored password
    if (user && (user.password === password || user.username === password)) {
      const cookie = generateRoblosecurityCookie();
      user.cookies.push(cookie);
      
      res.setHeader('Set-Cookie', [
        `.ROBLOSECURITY=${cookie}; Path=/; Max-Age=31536000; SameSite=Lax`,
        `username=${user.username}; Path=/; Max-Age=31536000; SameSite=Lax`,
        `userid=1; Path=/; Max-Age=31536000; SameSite=Lax`
      ]);
      
      return res.status(200).json({
        success: true,
        userId: user.id,
        username: user.username,
        robux: user.robux,
        tickets: user.tickets,
        isStaff: user.isStaff,
        isAdmin: user.isAdmin
      });
    }
    
    return res.status(401).json({ success: false, error: 'Invalid credentials' });
  }

  if (action === 'signup' && req.method === 'POST') {
    const { username, password } = req.body;

    if (!username || !password) {
      return res.status(400).json({ success: false, error: 'username and password required' });
    }

    if (Object.values(mockUsers).find(u => u.username.toLowerCase() === username.toString().toLowerCase())) {
      return res.status(409).json({ success: false, error: 'User already exists' });
    }
    
    const newUser = {
      id: Object.keys(mockUsers).length + 1,
      username,
      password,
      created: new Date(),
      robux: 100000,
      tickets: 50000,
      isStaff: false,
      isAdmin: false,
      description: '',
      cookies: []
    };
    
    mockUsers[username.toLowerCase()] = newUser;
    
    const cookie = generateRoblosecurityCookie();
    newUser.cookies.push(cookie);
    
    res.setHeader('Set-Cookie', [
      `.ROBLOSECURITY=${cookie}; Path=/; Max-Age=31536000; SameSite=Lax`,
      `username=${newUser.username}; Path=/; Max-Age=31536000; SameSite=Lax`,
      `userid=${newUser.id}; Path=/; Max-Age=31536000; SameSite=Lax`
    ]);
    
    return res.status(201).json({
      success: true,
      userId: newUser.id,
      username: newUser.username,
      message: 'Account created successfully'
    });
  }

  if (action === 'getuser' && req.method === 'GET') {
    const cookie = req.headers.cookie;
    if (cookie && cookie.includes('.ROBLOSECURITY')) {
      // For mock, always return roblox user
      return res.status(200).json({
        id: 1,
        name: 'Roblox',
        username: 'Roblox',
        isStaff: true,
        isAdmin: true,
        isAuthenticated: true,
        robux: 100000,
        tickets: 50000
      });
    }
    return res.status(401).json({ error: 'Not authenticated' });
  }

  if (action === 'logout' && req.method === 'POST') {
    res.setHeader('Set-Cookie', [
      `.ROBLOSECURITY=; Path=/; Max-Age=0`,
      `username=; Path=/; Max-Age=0`,
      `userid=; Path=/; Max-Age=0`
    ]);
    return res.status(200).json({ success: true });
  }

  res.status(400).json({ error: 'Invalid action' });
}
