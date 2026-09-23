# Open questions

This document contains unresolved product questions only.

Questions should be removed once answered and, when relevant, the resulting decision moved into the appropriate product document.

## Technical spike

### What response does Nantes return when there are no current loans?

The observed nonempty loans response contains an `items` array and a `total`. Verify the source response for zero loans so that successful empty data can be distinguished from an unexpected incomplete response.

## Later product questions

- reservation statuses and sorting;
- exact notification schedule;
- opening-hours model and exceptions;
- network and branch preference model;
- search ranking;
- series and next-volume suggestions.
