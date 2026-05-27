-- 74: Stripe self-service signup
--
-- Allows system-created pilot records without an admin user (self-service flow)
-- and adds subscription tracking for Stripe payment state.
--
-- Run on HostGator before deploying the Stripe integration.

ALTER TABLE ModernPilotBusiness
  MODIFY COLUMN UpdatedByAdminUserID INT NULL;

CREATE TABLE ModernBusinessSubscription (
  BusinessID              INT           NOT NULL,
  StripePlanKey           VARCHAR(20)   NULL,
  StripeCustomerId        VARCHAR(100)  NULL,
  StripeSubscriptionId    VARCHAR(100)  NULL,
  StripeCheckoutSessionId VARCHAR(150)  NULL,
  SubscriptionStatus      VARCHAR(30)   NOT NULL DEFAULT 'pending_payment',
  MaxClients              INT           NOT NULL DEFAULT 300,
  SubscriptionEndsAt      DATETIME      NULL,
  GraceEndsAt             DATETIME      NULL,
  CreatedViaSelfService   TINYINT(1)    NOT NULL DEFAULT 0,
  CreatedAt               DATETIME      NOT NULL,
  UpdatedAt               DATETIME      NOT NULL,
  PRIMARY KEY (BusinessID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
