# Running the TFTV wiki update (for team members)

This folder is a Claude Code skill. It brings https://wiki.phoenixpoint.com/Terror_from_the_Void up to
date with the latest public TFTV release. It writes the new page text and a change report to `wiki/`
on a branch for review, and never edits the wiki itself. `SKILL.md` holds the instructions Claude
follows. This file covers what you, a person, need to set up.

## Before you start

1. **GitHub:** you need write access to `Voland163/TFTV`, and your GitHub connected to your Claude
   account at https://claude.ai/connect-github. The Claude GitHub App is already installed on the repo.
2. **Discord bot token:** Claude reads the changelog in #announcements through a Discord bot. Either get
   the token of the existing TFTV bot privately from Voland (never through a Discord channel or a
   chat with Claude), or create your own bot. Your own bot needs only **View Channel** and **Read
   Message History** on #announcements and #open-beta-aircraft-rework.
3. **Where Claude runs:** pick one.
   - **Claude Code on the web (claude.ai/code):** start a session on `Voland163/TFTV`. When you give
     the prompt below, Claude will propose a cloud environment that allows `wiki.phoenixpoint.com` and
     `discord.com`, and it will open a secure form for the bot token. Approve both. To set this up by
     hand instead, open the environment settings, add both hosts under Network access, and add the token
     under Network secrets for host `discord.com` (Authorization header, "Bot" prefix). In later sessions,
     choose that environment.
   - **Claude Code on your computer:** clone the repo, `export DISCORD_BOT_TOKEN=<token>` (the bare token),
     and run `claude` in the repo folder.

## The prompt

> Use the tftv-wiki-update skill to update the TFTV wiki page to the latest public release. Commit and push to a branch as you go.

## What to expect

- Claude checks each change against the release's code and English localization, not just the
  changelog text. Every change it makes to the page is recorded in `wiki/LEDGER.md` with the code lines or
  localization key behind it, and a script verifies those citations before the work counts as done.
- It is a long job: about 2.5 years of patches against a 250 KB page. It will probably take several
  sessions. Progress is saved in `wiki/LEDGER.md` and `wiki/STATE.json` on the branch. To pick it up
  again, start a new session on that branch and say "continue the wiki update". Two people can work on
  the same branch if they take different sections.
- The result is a branch with `wiki/Terror_from_the_Void.wiki` (the full page), `wiki/CHANGES.md` (what
  changed and why, with sources and open questions) and `wiki/IMAGES.md` (images to upload first). The
  first commit on the branch is the live page, so the branch diff shows exactly what would change on
  the wiki.
- Publishing is done by a person with a wiki account. Follow "How to publish" in `wiki/CHANGES.md`.

## Later updates

After each new public release, run the same prompt again. The skill reads `wiki/STATE.json` and covers
only the releases since the last run.
