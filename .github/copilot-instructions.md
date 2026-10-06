# Copilot Instructions

## Project Guidelines
- User prefers Android OS-style permission/settings flow over a local boolean gate for internet-related consent behavior.
- Internet access must require explicit user opt-in before any network call is allowed.
- Volume Restore app must not request notification permission or display notifications.
- Volume Restore event monitoring must default to off until the user explicitly enables it; after enabling, the choice must persist across app launches.