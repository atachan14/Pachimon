# Skill Hit Runtime

## Purpose

`SkillHit` groups every effect produced by one hit against one target.
Target redirection and evasion are resolved once, before damage and Status
payloads are applied.

## Rules

- A damage-only attack may let the damage service create its Hit implicitly.
- A multi-component attack must call `BeginAttackHit` once and share that Hit.
- Damage and its attached Status applications share the same Hit.
- An area or chain attack creates one Hit for each target or chain step.
- A Status-only enemy effect uses `BeginStatusHit`.
- Status-only Hits can be redirected by Dragon Defense and can be evaded.
- Self effects and ally support effects do not use an attack Hit.

## Evasion

- Footwork is checked and consumed when the Hit is created.
- An evaded Hit cancels all damage and Status payloads belonging to that Hit.
- One evaded Hit publishes one `AttackEvadedEvent`, regardless of payload count.
- Status Damage and other effects that are not attacks do not consume Footwork.

## Current Boundary

`NoTarget` is not a Hit outcome. Skills continue to represent it with
`SkillResolution.WasTargetUnavailable` before creating a Hit.
