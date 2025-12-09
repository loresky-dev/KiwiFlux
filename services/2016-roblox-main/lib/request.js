import axios from 'axios';
import config from '../lib/config';
let _csrf = '';

const getFullUrl = (apiSite, fullUrl) => {
  return config.publicRuntimeConfig.backend.apiFormat.replace(/\{0\}/g, apiSite).replace(/\{1\}/g, fullUrl);
}

const getBaseUrl = () => {
  return config.publicRuntimeConfig.backend.baseUrl;
}

const getUrlWithProxy = (url) => {
  if (config.publicRuntimeConfig.backend.proxyEnabled)
    return '/api/proxy?url=' + encodeURIComponent(url);
  return url;
}

// Mock response handler for when backend is unavailable
const getMockResponse = (url) => {
  const urlStr = url.toString().toLowerCase();
  console.log('[MOCK] Getting mock response for:', urlStr);
  
  // Check authenticated endpoint first (most specific)
  if (urlStr.includes('/users/v1/users/authenticated')) {
    console.log('[MOCK] Returning authenticated user data');
    return {
      id: 1,
      name: 'roblox',
      displayName: 'roblox',
      username: 'roblox',
      isStaff: true,
      isAdmin: true,
      description: 'System Administrator'
    };
  }

  if (urlStr.includes('/users/v1/users/') && urlStr.includes('/status')) {
    return {
      status: 'Online'
    };
  }

  if (urlStr.includes('/users/v1/users/')) {
    // Generic user info endpoint
    return {
      id: 1,
      name: 'roblox',
      displayName: 'roblox',
      username: 'roblox',
      isStaff: true,
      isAdmin: true,
      description: 'System Administrator',
      created: new Date().toISOString()
    };
  }
  
  if (urlStr.includes('/chat/v2/chat-settings')) {
    return {
      chatEnabled: true,
      isActiveChatUser: true,
      maxMessageLength: 500,
      maxConversations: 100
    };
  }

  if (urlStr.includes('/chat/v2/get-user-conversations')) {
    // Return as array directly since d.data is expected to be the array
    return [];
  }

  if (urlStr.includes('/chat/v2/multi-get-latest-messages')) {
    // Return as array directly 
    return [];
  }

  if (urlStr.includes('/chat/v2/get-messages')) {
    // Return as array directly
    return [];
  }

  if (urlStr.includes('/chat/v2')) {
    // Catch-all for other chat endpoints
    return {
      success: true,
      data: []
    };
  }
  
  if (urlStr.includes('/api/alerts/alert-info')) {
    return {
      alerts: [],
      count: 0
    };
  }
  
  if (urlStr.includes('/economy/v1/users') && urlStr.includes('/robux')) {
    return {
      robux: 100000
    };
  }
  
  if (urlStr.includes('/economy/v1/users') && urlStr.includes('/tickets')) {
    return {
      tickets: 50000
    };
  }
  
  if (urlStr.includes('/catalog/v1/search')) {
    return {
      data: [],
      nextPageCursor: null,
      previousPageCursor: null,
      _total: 0
    };
  }

  if (urlStr.includes('/catalog/v1') && urlStr.includes('/details')) {
    return {
      data: []
    };
  }
  
  if (urlStr.includes('/friends/v1') && urlStr.includes('/statuses')) {
    return {
      data: [
        {
          userId: 1,
          status: 'Offline',
          lastLocation: ''
        }
      ]
    };
  }

  if (urlStr.includes('/friends/v1')) {
    return {
      data: [],
      nextCursor: ''
    };
  }
  
  if (urlStr.includes('/message-notification-stream')) {
    return {
      messages: []
    };
  }
  
  if (urlStr.includes('/game-passes')) {
    return {
      gamePassCount: 0
    };
  }
  
  if (urlStr.includes('/badges')) {
    return {
      badges: [],
      count: 0
    };
  }

  if (urlStr.includes('/game-sorts') || urlStr.includes('/gamesorts') || urlStr.includes('/games/sorts')) {
    return {
      sorts: [
        {
          name: 'Popular',
          displayName: 'Popular',
          token: 'popular',
          timeInterval: '1d'
        },
        {
          name: 'Trending',
          displayName: 'Trending',
          token: 'trending',
          timeInterval: '1h'
        }
      ]
    };
  }

  if (urlStr.includes('/game-list') || urlStr.includes('/games/list')) {
    return {
      games: [],
      nextCursor: ''
    };
  }

  if (urlStr.includes('/universes')) {
    return {
      id: 1,
      name: 'Game',
      description: 'Mock game',
      creator: { id: 1, name: 'roblox' },
      created: new Date().toISOString(),
      updated: new Date().toISOString()
    };
  }

  if (urlStr.includes('/icon') || urlStr.includes('/assets') || urlStr.includes('/thumbnails')) {
    return {
      data: [],
      url: ''
    };
  }

  if (urlStr.includes('/games/icons')) {
    // Return structure expected by multiGetUniverseIcons
    return {
      data: []
    };
  }
  
  return {
    success: true,
    data: []
  };
}

const request = async (method, url, data) => {
  const isBrowser = typeof window !== 'undefined';
  try {
    let headers = {
      'x-csrf-token': _csrf,
    }
    if (!isBrowser) {
      // Auth header, if required
      const authHeaderValue = config.serverRuntimeConfig.backend.authorization;
      if (typeof authHeaderValue === 'string')
        headers[config.serverRuntimeConfig.backend.authorizationHeader || 'authorization'] = authHeaderValue;
      // Custom user agent
      headers['user-agent'] = 'Roblox2016/1.0';
    }
    const result = await axios.request({
      method,
      url: getUrlWithProxy(url),
      data: data,
      headers: headers,
      maxRedirects: 0,
      timeout: 3000
    });
    return result;
  } catch (e) {
    // If network error and backend unavailable, return mock response
    const isNetworkError = !e.response && (
      e.code === 'ECONNREFUSED' || 
      e.code === 'ECONNABORTED' ||  // Timeout error
      e.code === 'ERR_NETWORK' || 
      e.message === 'Network Error' ||
      e.message.includes('ECONNREFUSED') ||
      e.message.includes('ECONNABORTED') ||
      e.message.includes('ERR_NETWORK') ||
      e.message.includes('timeout')  // Timeout message
    );
    
    if (isNetworkError) {
      console.warn('[NETWORK ERROR] Backend unavailable, using mock response for:', url);
      console.warn('[NETWORK ERROR] Error code:', e.code, 'Message:', e.message);
      return {
        status: 200,
        data: getMockResponse(url),
        headers: {}
      };
    }
    
    if (e.response) {
      let resp = e.response;
      if (resp.status === 403 && resp.headers['x-csrf-token']) {
        _csrf = resp.headers['x-csrf-token'];
        return await request(method, url, data);
      }
    }
    if (isBrowser) {
      // attempt to make errors easier to diagnose
      if (e.response) {
        // check for regular
        if (e.response.data && e.response.data.errors && e.response.data.errors.length) {
          let err = e.response.data.errors[0]
          e.message = e.message + ': ' + (err.code + ': ' + err.message);
        }
      }
      throw e;
    } else {
      throw new Error(e.message);
    }
  }
}

export default request;

export {
  getFullUrl,
  getBaseUrl,
  getUrlWithProxy,
}