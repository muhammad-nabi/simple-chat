---
title: "Product Brief Distillate: simple-chat"
type: llm-distillate
source: "product-brief-simple-chat.md"
created: "2026-03-27"
purpose: "Token-efficient context for downstream PRD creation"
---

# Product Brief Distillate: simple-chat

## Tech Stack & Architecture

- Backend: .NET Core with SignalR for real-time messaging
- Frontend: Angular, responsive web UI, PWA-capable
- Deployment: Docker Compose (app + database)
- Target memory footprint: under 512MB for teams up to 50 users
- Target audience range: 5-200 users per deployment
- License: MIT

## Competitive Intelligence

- **Mattermost**: Go + React. Free "Entry" tier (Oct 2025) caps at 10K viewable message history. Rebranded as "Intelligent Mission Environment" — moving upmarket. 2 containers + PostgreSQL, ~4GB RAM.
- **Rocket.Chat**: Node.js (Meteor). Community Edition has 25-user soft cap. Push notification limits on self-hosted are a common complaint. Requires MongoDB replica set, ~2GB RAM. Progressively paywalling features behind Enterprise in v7.x+.
- **Zulip**: Python (Django) + React. Fully free, no feature gating. But requires 5 services (app, PostgreSQL, Memcached, RabbitMQ, Redis), no rootless Docker. Topic-based threading has steep learning curve.
- **Element/Matrix**: Python (Synapse) or Rust (Conduit). Federation adds complexity. Synapse is memory-hungry (2-4GB+), slow for large rooms.
- **Key gap**: No self-hosted chat tool exists in the .NET ecosystem. Every competitor uses Go, Python, or Node.js.

## Scope Signals

### Confirmed V1 (must-have)
- User registration and authentication
- 1-on-1 private chat
- Group chat
- Real-time messaging (SignalR)
- Notifications
- File and image sharing
- Message search
- User presence/status (online, away, offline)
- Full message history persistence
- Responsive web UI with PWA support
- Basic admin controls (user management, roles)
- Docker Compose deployment

### Confirmed V1 Exclusions
- Video/voice calling
- Omnichannel integrations
- Plugin/extension system
- AI features
- Mobile native apps
- Federation
- End-to-end encryption
- Admin analytics dashboards
- LDAP/SSO integration
- Data import/migration tools
- Webhooks (deferred — user explicitly chose "later" over V1)

### Post-V1 Roadmap Signals
- Webhook integrations for DevOps workflows (user confirmed interest, deferred from V1)
- LDAP/Active Directory authentication for enterprise adoption
- Data import tools to ease migration from Slack/Mattermost
- Community governance model needed as adoption grows

## User & Market Context

- Product type: self-hostable tool for any team (not limited to one organization)
- Primary users: small-to-mid teams wanting simple internal comms without DevOps overhead
- Secondary users: .NET shops wanting a chat tool native to their stack
- Data sovereignty is an explicit positioning angle — no telemetry, no phone-home, compliance-friendly by architecture (GDPR, HIPAA, government)
- Long-term vision: grow from a tool into a community-driven project

## Adoption & Distribution Insights (from review)

- Discovery channels: Awesome-Selfhosted GitHub list, r/selfhosted (500K+ members), .NET community blogs/conferences, Hacker News
- Deployment marketplaces: Azure Marketplace, DigitalOcean 1-Click, Railway
- Success metric: first 100 active deployments within 6 months of public release
- The "simplicity backlash" trend is real — teams are actively seeking minimal tools as major platforms add complexity
- Free tier erosion in competitors creates a specific switching trigger for price-sensitive teams

## Rejected Ideas & Decisions

- AGPL license rejected — MIT chosen for maximum adoption and contribution friendliness
- Webhooks in V1 rejected — keeps V1 scope tight, deferred to post-V1
- Plugin/extension system explicitly out of scope — simplicity is the moat, not extensibility
- No plans for a hosted/SaaS offering — self-hosted only is the positioning

## Open Questions (unresolved during discovery)

- Database choice not specified — PostgreSQL is the natural fit for .NET Core + Docker, but SQLite for small deployments could lower the bar further
- File storage backend and limits not defined — this is the most common operational headache in self-hosted chat (disk usage, backup bloat)
- Authentication mechanism details — local accounts only for V1, or any OAuth/social login?
- Community governance model — "radical simplicity" and "community-driven roadmap" are in tension; no framework yet for what gets in vs. stays out
- Upgrade/migration path between versions not discussed
- Backup and restore strategy not defined
