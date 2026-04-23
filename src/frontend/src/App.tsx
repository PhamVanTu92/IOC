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

// WS URL: dùng cùng host/port với trang web (qua nginx proxy)
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

// ─────────────────────────────────────────────────────────────────────────────

export function App() {
  return (
    <ApolloProvider client={apolloClient}>
      <AppShell />
    </ApolloProvider>
  );
}
