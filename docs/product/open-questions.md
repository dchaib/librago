# Open questions

This document contains unresolved product questions only.

Questions should be removed once answered and, when relevant, the resulting decision moved into the appropriate product document.

## Technical spike

### How does account synchronization fail?

Determine whether failures can occur independently per account and what can be reliably detected.

### What is the source behavior for zero loans?

Verify that connectors can distinguish:
- a successful response with zero loans;
- authentication/session failure;
- unexpected or partial response.

### What synchronization cadence is appropriate?

Version 0.1 requires automatic synchronization, but cadence is not yet specified.

## Later product questions

- reservation statuses and sorting;
- exact notification schedule;
- opening-hours model and exceptions;
- network and branch preference model;
- search ranking;
- series and next-volume suggestions.
