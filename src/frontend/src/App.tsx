import React from 'react';
import { ApolloClient, InMemoryCache, ApolloProvider, split, HttpLink } from '@apollo/client';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { createClient } from 'graphql-ws';
import { getMainDefinition } from '@apollo/client/utilities';
import { pluginRegistry } from '@core/PluginRegistry';
import { AppShell } from '@core/AppShell';
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
    return definition.kind === 'OperationDefinition' && definition.operation === 'subscription';
  },
  wsLink,
  httpLink
);

const apolloClient = new ApolloClient({
  link: splitLink,
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
        <AppShell />
      </ApolloProvider>
    </ErrorBoundary>
  );
}
