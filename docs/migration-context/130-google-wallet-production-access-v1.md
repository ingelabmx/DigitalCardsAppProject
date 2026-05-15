# 130 Google Wallet Production Access v1

## Summary

Google Wallet cards now keep the Puntelio branding order aligned with the product UI: the card title is the public business name and the program appears directly below it.

## Google Demo Mode

Google Wallet adds the `[TEST ONLY]` / `[SOLO PARA PRUEBAS]` prefix while the issuer account is in Demo Mode. This text is not part of the Puntelio payload and cannot be removed from code.

To remove it in production:

1. Complete the Google Pay & Wallet Console business profile.
2. Keep at least one Wallet class created for the issuer.
3. Request publishing access from the Google Wallet API console.
4. After approval, refresh or reopen installed cards so Google removes the demo prefix.

Official reference: https://developers.google.com/wallet/generic/test-and-go-live/request-publishing-access

## Notes

- No SQL is required.
- Existing cards still require a Google Wallet patch/refresh to pick up text changes.
- The visible QR text remains the client username.
