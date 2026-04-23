import React from 'react';
import { ApolloClient, InMemoryCache, ApolloProvider, split, HttpLink } from '@apollo/client';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { createClient } from 'graphql-ws';
import { getMainDefinition } from '@apollo/client/utilities';
import { pluginRegistry } from '@core/PluginRegistry';
import { AppShell } from '@core/AppShell';

// Import và đăng ký tất cả plugins
import { FinancePlugin } from '@plugins/finance';
import { HRPlugin } from '@plugins/hr';
import { MarketingPlugin } from '@plugins/marketing';

// ─────────────────────────────────────────────────────────────────────────────
// Đăng ký plugins vào registry
// ─────────────────────────────────────────────────────────────────────────────

pluginRegistry.register(FinancePlugin);
pluginRegistry.register(HRPlugin);
pluginRegistry.register(MarketingPlugin);

// ─────────────────────────────────────────────────────────────────────────────
// Apollo Client — hỗ trợ cả HTTP queries + WebSocket subscriptions
// ─────────────────────────────────────────────────────────────────────────────

const httpLink = new HttpLink({ uri: '/graphql' });

const wsLink = new GraphQLWsLink(
  createClient({ url: 'ws://localhost:5000/graphql' })
);

// Queries/Mutations → HTTP; Subscriptions → WebSocket
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
