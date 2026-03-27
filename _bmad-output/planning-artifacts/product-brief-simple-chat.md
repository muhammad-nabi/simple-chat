---
title: "Product Brief: simple-chat"
status: "complete"
created: "2026-03-27"
updated: "2026-03-27"
inputs: [user interview, market research]
---

# Product Brief: simple-chat

## Executive Summary

simple-chat is a self-hostable, real-time chat application built for teams that want internal communication without the overhead. It provides private messaging, group chat, file sharing, presence, search, and notifications — and nothing else.

The internal chat market has a bloat problem. Tools like Mattermost, Rocket.Chat, and Zulip started simple but have grown into sprawling platforms with omnichannel support, AI agents, marketplace plugins, and complex deployment requirements. Meanwhile, their free tiers are shrinking — Mattermost now caps message history at 10K, Rocket.Chat limits free workspaces to 25 users. Teams wanting a straightforward chat tool are caught between overengineered open-source platforms and restrictive SaaS pricing.

simple-chat fills that gap: a clean, focused, self-hosted messaging tool that any team can deploy and run without dedicated infrastructure expertise. Built on .NET Core and Angular, it is the only self-hosted chat tool native to the Microsoft ecosystem — no Go, Python, or Node.js runtimes to manage. No artificial limits. No feature creep. Just chat that works.

## The Problem

Internal teams need to communicate in real time. The options today are:

- **Enterprise SaaS** (Slack, Teams) — powerful but expensive, vendor-locked, and increasingly bloated with features most teams never touch.
- **Self-hosted platforms** (Mattermost, Rocket.Chat, Zulip, Matrix) — free in theory, but operationally heavy. Zulip requires 5 services. Matrix/Synapse consumes 2-4GB RAM. Rocket.Chat needs a MongoDB replica set. Each demands specialized knowledge to maintain.
- **Ad-hoc solutions** (email threads, SMS groups) — used when proper tools feel like overkill, but terrible for threaded conversation, history, and team visibility.

The common frustration: teams that just want to send messages end up administering platforms designed for enterprises with dedicated DevOps teams. And the free tiers they relied on keep getting smaller.

## The Solution

simple-chat delivers the core of what teams actually use daily:

- **Private messaging** — direct 1-on-1 conversations
- **Group chat** — create and manage group conversations
- **Real-time delivery** — instant message delivery via SignalR with sub-second latency
- **Notifications** — stay informed without being glued to the screen
- **File & image sharing** — share what you need in context
- **Message search** — find anything from your full conversation history
- **User presence** — see who's online, away, or offline
- **Responsive web UI** — works across desktop and mobile browsers, installable as a PWA

Deployable via Docker Compose with minimal infrastructure. Register users, start chatting. That's it.

## What Makes This Different

**.NET ecosystem — the only native option.** There is no self-hosted chat tool built on .NET Core today. For organizations already running .NET infrastructure, simple-chat fits their operational stack, tooling, and team skills without introducing foreign runtimes. This is not a nice-to-have — it is a genuine gap in the market. Licensed under MIT for maximum adoption and contribution.

**Radical simplicity.** While competitors race to become platforms, simple-chat stays a chat tool. There is no plugin marketplace, no omnichannel routing, no AI assistant, no video conferencing. The feature set is intentionally bounded.

**No artificial limits.** Full message history, no user caps, no feature gating between free and paid tiers. Self-host it and it's yours completely.

**Data sovereignty by default.** Every byte of data stays on infrastructure you control — no telemetry, no phone-home, no external dependencies. For teams in regulated industries or organizations with strict data residency requirements (GDPR, HIPAA, government), simple-chat is compliant by architecture, not by policy.

**Low operational overhead.** Single Docker Compose deployment with the application and a database. Designed for teams without dedicated DevOps — deploy it, run it, forget about it.

## Who This Serves

**Primary:** Small-to-mid-sized teams (5-200 people) that need internal real-time communication and prefer to self-host. They value simplicity over features and want a tool that fits their existing infrastructure without demanding specialized knowledge.

**Secondary:** .NET shops specifically — development teams and organizations already invested in the Microsoft ecosystem who want a chat tool that doesn't introduce foreign runtime dependencies.

## Success Criteria

- Teams can go from `docker-compose up` to chatting in under 15 minutes
- Real-time message delivery with P99 latency under 200ms
- Memory footprint under 512MB for teams up to 50 users
- First 100 active deployments within 6 months of public release
- Listed on Awesome-Selfhosted and visible in .NET community channels

## Scope

**V1 includes:** User registration and authentication, 1-on-1 private chat, group chat, real-time messaging (SignalR), notifications, file and image sharing, message search, user presence/status, full message history persistence, responsive web UI with PWA support, basic admin controls (user management, roles), Docker Compose deployment.

**V1 excludes:** Video/voice calling, omnichannel integrations, plugin/extension system, AI features, mobile native apps, federation, end-to-end encryption, admin analytics dashboards, LDAP/SSO integration, data import/migration tools.

## Vision

simple-chat starts as a tool and grows into a community. The initial goal is a focused, high-quality chat application that earns trust through reliability and simplicity. As adoption grows, the project becomes community-driven — contributors shape the roadmap, and the tool evolves based on what real teams actually need, not what looks good on a feature comparison chart.

The path forward includes webhook integrations for DevOps workflows, LDAP/Active Directory authentication for enterprise adoption, and data import tools to ease migration from existing platforms. The north star is being the go-to self-hosted chat tool for teams that refuse to trade simplicity for capability they'll never use.
