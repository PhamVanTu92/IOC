import React from 'react';
import {
  ApolloClient,
  InMemoryCache,
  ApolloProvider,
  split,
  HttpLink,
} from '@apollo/client';
import { setContext } from '@apollo/client/link/context';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { createClient } from 'graphql-ws';
import { getMainDefinition } from '@apollo/client/utilities';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { pluginRegistry } from '@core/PluginRegistry';
import { AppShell } from '@core/AppShell';
import { LoginPage } from '@/features/auth/LoginPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { FinancePlugin } from '@plugins/finance';
import { HRPlugin } from '@plugins/hr';
import { MarketingPlugin } from '@plugins/marketing';

// ── Đăng ký plugins ──────────────────────────────────────────────────────────
pluginRegistry.register(FinancePlugin);
pluginRegistry.register(HRPlugin);
pluginRegistry.register(MarketingPlugin);

// ── Apollo Client — HTTP + WebSocket ─────────────────────────────────────────
const httpLink = new HttpLink({ uri: '/graphql' });

const wsProtocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
const wsLink = new GraphQLWsLink(
  createClient({ url: `${wsProtocol}//${window.location.host}/graphql` })
);

const splitLink = split(
  ({ query }) => {
    const definition = getMainDefinition(query);
    return (
      definition.kind === 'OperationDefinition' && definition.operation === 'subscription'
    );
  },
  wsLink,
  httpLink
);

// Auth link — reads token from localStorage directly (runs outside React tree)
// Zustand persist key: 'ioc:auth', token is nested under state.token
const authLink = setContext((_, { headers }: { headers: Record<string, string> }) => {
  const raw = localStorage.getItem('ioc:auth');
  let token: string | null = null;
  if (raw) {
    try {
      const parsed = JSON.parse(raw) as { state?: { token?: string } };
      token = parsed?.state?.token ?? null;
    } catch {
      // ignore malformed data
    }
  }
  return {
    headers: {
      ...headers,
      ...(token ? { authorization: `Bearer ${token}` } : {}),
    },
  };
});

const apolloClient = new ApolloClient({
  link: authLink.concat(splitLink),
  cache: new InMemoryCache(),
  defaultOptions: {
    watchQuery: { fetchPolicy: 'cache-and-network' },
  },
});

// ── Error Boundary ────────────────────────────────────────────────────────────

interface ErrorBoundaryState {
  error: Error | null;
}

class ErrorBoundary extends React.Component<
  { children: React.ReactNode },
  ErrorBoundaryState
> {
  state: ErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    console.error('[IOC] Uncaught render error:', error, info.componentStack);
  }

  render() {
    if (this.state.error) {
      return (
        <div className="ioc-error">
          <div className="ioc-error__title">⚠️ Lỗi khởi động IOC</div>
          <pre className="ioc-error__message">
            {this.state.error.message}
            {'\n\n'}
            {this.state.error.stack?.split('\n').slice(1, 6).join('\n')}
          </pre>
          <button
            className="ioc-error__btn"
            onClick={() => window.location.reload()}
          >
            Tải lại trang
          </button>
        </div>
      );
    }
    return this.state.error === null ? this.props.children : null;
  }
}

// ─────────────────────────────────────────────────────────────────────────────

export function App() {
  return (
    <ErrorBoundary>
      <ApolloProvider client={apolloClient}>
        {/*
          BrowserRouter lives here so /login and /register are accessible
          OUTSIDE the protected AppShell, while AppShell handles all other routes.
        */}
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            {/* All other routes pass through the protected AppShell */}
            <Route path="/*" element={<AppShell />} />
          </Routes>
        </BrowserRouter>
      </ApolloProvider>
    </ErrorBoundary>
  );
}
