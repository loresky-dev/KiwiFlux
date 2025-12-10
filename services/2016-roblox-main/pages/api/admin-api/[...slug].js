import axios from 'axios';
import { getBaseUrl } from '../../../lib/request';

const backendBase = 'http://localhost:5000/admin-api/api/' // default backend admin endpoint
const AllAccess = [
  'GetStats',
  'GetAlert',
  'SetAlert',
  'CreateUser',
  'GetPendingGroupIcons',
  'GetAssetModerationDetails',
  'GetPendingModerationItems',
  'GetPendingModerationGameIcons',
  'SetGameIconModerationStatus',
  'SetAssetModerationStatus',
  'SetGroupIconModerationStatus',
  'GetGroupManageInfo',
  'GetUserJoinCount',
  'GetUsersList',
  'GetUserDetailed',
  'UnbanUser',
  'BanUser',
  'CreateMessage',
  'GetAdminMessages',
  'NullifyPassword',
  'DestroyAllSessionsForUser',
  'LockAccount',
  'RegenerateAvatar',
  'ResetAvatar',
  'GetAdminLogs',
  'GetUserBadges',
  'GiveUserBadge',
  'DeleteUserBadge',
  'GiveUserRobux',
  'GetUserCollectibles',
  'RemoveUserItem',
  'TrackItem',
  'GiveUserItem',
  'DeleteUser',
  'GetPreviousUsernames',
  'DeleteUsername',
  'DeleteComment',
  'DeleteForumPost',
  'RequestAssetReRender',
  'GetProductDetails',
  'SetAssetProduct',
  'CreateAsset',
  'CreateClothingAsset',
  'CopyClothingFromRoblox',
  'CreateAssetVersion',
  'MigrateAssetFromRoblox',
  'CreateGameForUser',
  'RequestWebsiteUpdate',
  'RunLottery',
  'GetUserStatusHistory',
  'DeleteUserStatus',
  'GetUserCommentHistory',
  'ManageFeatureFlags',
  'GetUsersOnline',
  'GetUsersInGame',
  'GetUserTransactions',
  'ResetUsername',
  'ResetDescription',
  'ManageApplications',
  'ClearApplications',
  'ManageInvites',
  'GetGroupWall',
  'DeleteGroupWallPost',
  'GetAllAssetComments',
  'GetAllUserStatuses',
  'LockAndUnlockGroup',
  'GetGroupStatus',
  'DeleteGroupStatus',
  'ResetGroup',
  'LockForumThread',
  'ManageReports',
  'GetAllAssetOwners',
  'GetDetailsFromThumbnail',
  'SetPermissions',
  'GetGameServers',
  'MakeItemLimited',
  'CreateAssetCopiedFromRoblox',
  'CreateBundleCopiedFromRoblox',
  'GetSaleHistoryForAsset',
  'RefundAndDeleteFirstPartyAssetSale',
];

// Default in-memory staff permissions for local dev. This grants *all* permissions to userId
// 1 (our mock 'Roblox' account) so the admin UI can be fully explored without the backend.
// Adjust or remove this mapping only for testing; this should not be used in production.
const staffPermissionsByUserId = {
  '1': AllAccess.slice().map(p => ({ userId: 1, permission: p }))
};

const mockResponses = (req, res, path, query) => {
  // Normalize path
  const p = '/' + path.join('/');
  const pNormalized = p.replace(/^\/api\//, '/');

  if (p === '/permissions' || pNormalized === '/permissions' || p === '/api/permissions') {
    return res.status(200).json({ data: { rank: {
      name: 'Owner',
      details: {
        isAdmin: true,
        isModerator: true,
        isOwner: true,
      },
      permissions: AllAccess
    }}});
  }
  if (p === '/staff/list' || pNormalized === '/staff/list' || p === '/api/staff/list') {
    return res.status(200).json({ data: [ { userId: 1 } ] });
  }

  if (p === '/staff/permissions' || pNormalized === '/staff/permissions' || p === '/api/staff/permissions') {
    const uid = query.userId || query.userId || (req.query && req.query.userId);
    if (req.method === 'GET') {
      const u = staffPermissionsByUserId[String(uid)] || [];
      return res.status(200).json({ data: u });
    }
    if (req.method === 'POST') {
      // Add a permission
      const q = req.url.includes('?') ? Object.fromEntries(new URLSearchParams(req.url.split('?')[1])) : {};
      const toAdd = q.permission;
      const uId = q.userId || uid;
      if (!staffPermissionsByUserId[uId]) staffPermissionsByUserId[uId] = [];
      if (!staffPermissionsByUserId[uId].find(x => x.permission === toAdd)) {
        staffPermissionsByUserId[uId].push({ userId: Number(uId), permission: toAdd });
      }
      return res.status(200).json({ success: true });
    }
    if (req.method === 'DELETE') {
      const q = req.url.includes('?') ? Object.fromEntries(new URLSearchParams(req.url.split('?')[1])) : {};
      const toDel = q.permission;
      const uId = q.userId || uid;
      if (!staffPermissionsByUserId[uId]) staffPermissionsByUserId[uId] = [];
      staffPermissionsByUserId[uId] = staffPermissionsByUserId[uId].filter(x => x.permission !== toDel);
      return res.status(200).json({ success: true });
    }
  }

  if (p === '/staff/permissions/list' || pNormalized === '/staff/permissions/list' || p === '/api/staff/permissions/list') {
    return res.status(200).json({ data: AllAccess });
  }

  if (p.startsWith('/assets')) {
    // For assets/get-asset-stream provide a small placeholder PNG
    const imgBase64 = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4//8/AwAI/AL+f9zMAAAAAElFTkSuQmCC';
    const b = Buffer.from(imgBase64, 'base64');
    res.setHeader('content-type', 'image/png');
    return res.status(200).send(b);
  }

  // Simple in-memory applications store for admin panel
  if (!global.__mockApplications) {
    global.__mockApplications = [
      {
        id: 'app-1',
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(),
        about: 'I would like to join to collaborate on events.',
        socialPresence: 'https://twitter.com/roblox',
        verifiedUrl: 'https://twitter.com/roblox',
        isVerified: true,
        verificationPhrase: 'verifyme123',
        userId: 1,
        status: 'Pending',
      },
      {
        id: 'app-2',
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 48).toISOString(),
        about: 'I represent a community that wants to collaborate',
        socialPresence: 'https://www.youtube.com/channel/abcd',
        verifiedUrl: 'https://www.youtube.com/channel/abcd',
        isVerified: true,
        verificationPhrase: 'hello',
        userId: 2,
        status: 'Approved',
      },
      {
        id: 'app-3',
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 72).toISOString(),
        about: 'I have a blog where we discuss games',
        socialPresence: 'http://roblox.com/users/123',
        verifiedUrl: '',
        isVerified: false,
        verificationPhrase: 'phrase-abc',
        userId: 3,
        status: 'Rejected',
        rejectionReason: 'Poor social links',
      },
      {
        id: 'app-4',
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 7).toISOString(),
        about: 'Old application',
        socialPresence: 'https://instagram.com/test',
        verifiedUrl: '',
        isVerified: false,
        verificationPhrase: 'phrase-123',
        userId: 4,
        status: 'SilentlyRejected',
      }
    ];
  }

  // Applications endpoints
  if (p === '/applications/list' || pNormalized === '/applications/list' || p === '/api/applications/list') {
    const limit = 10;
    const offset = Number(query.offset || 0);
    const status = query.status;
    let items = global.__mockApplications.slice().reverse(); // newest first
    if (status && status !== 'All') {
      items = items.filter((a) => a.status === status);
    }
    // search filtering (simple contains)
    if (query.searchColumn && query.searchQuery) {
      items = items.filter((a) => (a[(query.searchColumn || '').toString().toLowerCase()] || '').toString().toLowerCase().includes((query.searchQuery || '').toString().toLowerCase()));
    }
    const page = items.slice(offset, offset + limit);
    return res.status(200).json(page);
  }

  if (p === '/applications/details' || pNormalized === '/applications/details' || p === '/api/applications/details') {
    const id = query.id;
    const entry = (global.__mockApplications.find((a) => a.id === id) || null);
    if (!entry) return res.status(404).json({ error: 'Not found' });
    // Return application object directly as the admin client expects
    return res.status(200).json(entry);
  }

  if (p === '/applications/update-lock' || pNormalized === '/applications/update-lock' || p === '/api/applications/update-lock') {
    // ignore locking for mock; return success
    return res.status(200).json({ success: true });
  }

  // app actions: approve, decline, decline-silent, clear
  const appActionMatch = p.match(/^\/applications\/(.*)$/);
  if (appActionMatch) {
    const rest = appActionMatch[1];
    // Examples: app-1/approve, app-1/decline
    const [appId, action] = rest.split('/');
    const app = global.__mockApplications.find((a) => a.id === appId);
    if (app) {
      if (action === 'approve') {
        app.status = 'Approved';
        return res.status(200).json({ success: true });
      }
      if (action === 'decline') {
        app.status = 'Rejected';
        const q = req.url.includes('?') ? Object.fromEntries(new URLSearchParams(req.url.split('?')[1])) : {};
        app.rejectionReason = q.reason || 'No reason provided';
        return res.status(200).json({ success: true });
      }
      if (action === 'decline-silent') {
        app.status = 'SilentlyRejected';
        return res.status(200).json({ success: true });
      }
      if (action === 'clear') {
        app.matrixName = '[ Content Deleted ]';
        app.matrixDomain = '[ Content Deleted ]';
        app.about = '[ Content Deleted ]';
        app.socialPresence = '[ Content Deleted ]';
        return res.status(200).json({ success: true });
      }
    }
    return res.status(404).json({ error: 'Application not found' });
  }

  // default fallback
  return res.status(200).json({ data: [] });
}

export default async function handler(req, res) {
  // Join slug parts to create path
  const { slug = [] } = req.query;
  const pathParts = Array.isArray(slug) ? slug : [slug];

  const backendUrl = backendBase + pathParts.join('/');

  // Try to forward to real server
  try {
    const headers = { 'x-csrf-token': req.headers['x-csrf-token'] || '', cookie: req.headers['cookie'] || '' };
    const method = req.method.toLowerCase();
    const axiosConfig = {
      method: method,
      url: backendUrl + (req.url.includes('?') ? '?' + req.url.split('?')[1] : ''),
      data: req.body,
      headers,
      validateStatus: () => true,
      responseType: 'arraybuffer',
      timeout: 3000,
    };
    const result = await axios.request(axiosConfig);
    // If backend returned 5xx/4xx we still forward the result
    for (const header of Object.keys(result.headers || {})) {
      try { res.setHeader(header, result.headers[header]); } catch (e) {}
    }
    res.status(result.status).send(result.data);
  } catch (e) {
    // Backend unavailable -> serve mock responses
    return mockResponses(req, res, pathParts, req.query);
  }
}

export const config = {
  api: {
    bodyParser: false,
  },
}
