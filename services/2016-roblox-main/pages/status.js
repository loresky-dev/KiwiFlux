import { useEffect, useState } from 'react';
import AuthenticationStore from '../stores/authentication';

export default function StatusPage() {
  const auth = AuthenticationStore.useContainer();
  const [cookies, setCookies] = useState(null);

  useEffect(() => {
    setCookies(document.cookie);
  }, []);

  return (
    <div style={{ padding: '20px', fontFamily: 'monospace' }}>
      <h1>System Status</h1>
      
      <h2>Authentication</h2>
      <p><strong>User:</strong> {auth.username || 'Not logged in'}</p>
      <p><strong>User ID:</strong> {auth.userId || 'N/A'}</p>
      <p><strong>Authenticated:</strong> {auth.isAuthenticated ? 'Yes ✓' : 'No ✗'}</p>
      <p><strong>Loading:</strong> {auth.isPending ? 'Yes' : 'No'}</p>
      <p><strong>Robux:</strong> {auth.robux?.toLocaleString() || 'N/A'}</p>
      <p><strong>Tickets:</strong> {auth.tix?.toLocaleString() || 'N/A'}</p>

      <h2>Cookies</h2>
      <pre>{cookies || 'Loading...'}</pre>

      <h2>Available Pages</h2>
      <ul>
        <li><a href="/">/</a> - Home</li>
        <li><a href="/home">/home</a> - Dashboard</li>
        <li><a href="/catalog">/catalog</a> - Catalog</li>
        <li><a href="/games">/games</a> - Games</li>
        <li><a href="/develop">/develop</a> - Develop</li>
      </ul>

      <h2>Auto-Login</h2>
      <p>To auto-login as 'roblox' on startup, the app automatically calls the login API.</p>
      <p>Current status: {auth.isAuthenticated ? '✓ Logged in' : '✗ Not logged in'}</p>
    </div>
  );
}
