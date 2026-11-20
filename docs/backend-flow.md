# Backend flow

## Manual matching

1. `POST /api/v1/trips`: provide rider, addresses and coordinates. Matching/Pricing calls OSRM and returns the road estimate and fare. No driver is invited yet.
2. `POST /api/v1/matching/match` with `{ "tripId": "..." }`: explicitly request a driver. The response includes `matchSessionId`, `driverId`. Matching saves and publishes the invitation; Trip stores it asynchronously.
3. `GET /api/v1/trips/{id}`: inspect the quote, current offer, rejection count and failure reason. Wait until `offeredDriverId` matches the Matching response before accepting/rejecting.
4. `POST /api/v1/trips/{id}/driver-accept` or `driver-reject`: send `{ "driverId": "..." }` using the current invitation.
5. After rejection, call Matching manually again. The backend does not automatically find another driver. Repeated rejection of the same invitation counts once. Repeated matching while an invitation is pending returns that invitation. After rejection/cancellation, Matching updates its own session from events. Calling again before those events arrive can still return the previous invitation; it cannot be accepted in Trip.
6. After acceptance: `POST .../pickup`, then `.../dropoff` with `{ "paymentMethod": "Wallet" }` or `{ "paymentMethod": "Card", "gatewayToken": "mock-success" }`.
7. Payment is asynchronous. Read the trip and its payment until Trip reaches `Completed` or Payment records a failure. Temporary processing errors are retried automatically with the same idempotency key. After a recorded failure, call `POST /api/v1/payments` with the trip ID, payment method and a new idempotency key. Include a gateway token for card. The endpoint uses the first payment's participants and amount and returns HTTP 409 if any payment for the trip is settled or still pending.

There is no price confirmation endpoint, offer expiry or additional authorization policy in this change.

## Configurable rejection limit

`PricingConfig:MaxAllowedRejections` in Matching is k. It must be >= 0 and is copied into the matching session when the quote is created. Existing quotes keep their original limit; restart Matching after changing configuration; new quotes then use the new limit.

For k = 1: A rejects -> count 1, trip stays Requested. Manually match B. B accepts -> normal trip; B rejects -> count 2 > 1, trip becomes Failed. No further matching is allowed. No available driver returns an API error while Trip remains Requested and Matching remains Searching; the user can call Matching again later.

## Road routing

`Routing:BaseUrl` defaults to `https://router.project-osrm.org`; `Routing:Profile` defaults to `driving` (car). Stock OSRM has car/bicycle/foot profiles, not a standard motorcycle profile. To use motorcycles, configure an OSRM endpoint built with a motorcycle profile and set the matching profile name. Changing only the URL profile segment does not create a motorcycle road dataset.

Coordinates are sent as longitude,latitude. OSRM distance is converted from meters to km; duration from seconds to rounded-up minutes. Client `distanceKm` / `estimatedMinutes` legacy fields are ignored for pricing. There is no Haversine or fixed-fare fallback. Routing unavailability returns HTTP 503 through Trip and creates no Trip record. The public OSRM server is a demo dependency: for a dependable presentation use an OSRM endpoint you control. OSRM duration is an estimate, not live traffic ETA.

References: [HTTP API](https://github.com/Project-OSRM/osrm-backend/blob/master/docs/http.md), [profiles](https://github.com/Project-OSRM/osrm-backend/blob/master/docs/profiles.md).

## State and consistency

`Requested -> DriverAccepted -> PickedUp -> DroppedOff -> PaymentPending -> Completed`.
`DroppedOff` is the domain transition immediately before starting payment in the same save. API readers normally see PaymentPending after dropoff. Cancellation is allowed before pickup only. Matching failure uses Failed + FailureReason. Payment failures leave Trip at PaymentPending; individual outcomes are owned by Payment.

Trip uses transactional outbox, without an application Version field. Only the offered driver and offer ID can accept/reject; these are business checks, not login authorization. Matching keeps invitation history and atomically reserves a driver so two trips cannot take the same available driver. A stale release is scoped to its trip/offer and cannot free a driver assigned to another trip.

Dropoff moves Trip to PaymentPending and publishes an event with an explicit IdempotencyKey generated once for the payment attempt. Event Id and transport MessageId identify messages, not payment attempts. Automatic retries reuse that event and IdempotencyKey. Manual RabbitMQ replay tests must retain message.idempotencyKey even when changing event Id or transport MessageId. Duplicate-key results are logged at Warning. Trip stores no payment request ID. Successful payment events identify the trip by TripId. Payment owns individual transaction outcomes and handles user-triggered retries for trips with a recorded failed payment.

## Upgrade and demo data

The Trip migration adds offer/payment fields, renames QuoteId to MatchSessionId and translates legacy status names. Startup applies pending migrations. Existing legacy trips had randomly generated QuoteId values that do not identify MongoDB sessions: create new trips for the new matching flow. The RemoveTripPaymentRequestId migration removes the obsolete request ID column and synchronizes the earlier removal of FinalFare from the domain; completed trips remain completed.

MongoDB creates `UX_MatchSession_TripId`. If legacy data contains duplicate TripId sessions, index initialization fails with a clear error; reconcile those duplicates before starting Matching. No data is silently deleted.

Synthetic Trip seed records have been removed because their quotes did not exist in Matching. Driver registration still supplies demo driver locations. Identity remains an existing auxiliary service; together with the three core services this is four application services, so discuss the assignment's service-count limit separately.

## Validation

Build: `dotnet build SwiftRide-Microservice.slnx`.
Unit tests: run the three `*.UnitTests.csproj` projects under `src/tests`.
Integration tests: run the three `*.IntegrationTests.csproj` projects with Docker Engine running; they use real SQL Server, MongoDB and PostgreSQL containers.

## Request validation

FluentValidation checks request input: required IDs, coordinates, string lengths and enum values. It does not load entities or check database state.

Each handler reads its own repository and explicitly returns NotFoundError (HTTP 404) when a resource is missing, or ConflictError (HTTP 409) when the state/action is invalid. This includes trip acceptance/rejection, cancellation, pickup/dropoff, payment queries and matching queries. There are no RequireEntity/RequireCondition helpers or scoped entity caches.

The MediatR ValidationBehavior still runs input validators before handlers and returns HTTP 400 with field errors. Transaction checks, idempotency and domain transitions remain explicit application/domain logic.

## Simplified Matching

Matching never calls Trip to fetch its status. It reads its own MongoDB session, selects a driver, saves the invitation and publishes DriverFoundEvent. There is no ITripOfferGateway, MatchingRequestState, request/response contract or ValidatedEntity in Matching.

Trip owns trip status and validates each received invitation. Accepted/rejected/cancelled events update Matching asynchronously. If Trip cannot accept a proposal, DriverOfferRejectedEvent releases that specific driver reservation; stale events cannot release a newer reservation. Replaying an already-rejected proposal is ignored while the original rejection event is delivered.

Matching's RejectCount comes from its local invitation history. FluentValidation still validates incoming requests; domain transition checks and driver reservations remain in place. Matching is always called manually.

## Removing application Version fields

Trip and MatchSession no longer have application Version properties or optimistic-version checks. A new SQL migration drops Trips.Version even if the earlier migration was already applied. Matching startup removes legacy version fields from its MongoDB sessions. MassTransit inbox/outbox RowVersion fields are internal infrastructure and are unchanged.

Invited-driver checks, state guards, payment idempotency and driver reservations are retained. There is no application-version protection against two requests updating the same trip/session at the same time.

When the nearest candidate is taken by another trip between selection and reservation, Matching excludes that candidate and searches again within the same API call. Only successful reservations create a driver invitation. Reservation conflicts do not increment RejectCount or consume the allowed rejection limit. If no available candidate remains, the API returns a no-driver error without ending the session.

A new SQL migration removes the unused PaymentRequestFingerprint and RetrySamePaymentRequest fields. Automatic broker retries and payment idempotency remain enabled.

TripAcceptedConsumer and DriverRejectedConsumer each handle their own event directly, in separate files. They load the matching session, check the current offer/state, update the invitation outcome and save through MatchingRepository. The repository has no combined accept/reject business helper. Rejection still releases the exact driver reservation, including duplicate-event retries, and never triggers an automatic rematch.

Trip no longer stores LastRejectedOfferId, LastRejectedDriverId, PaymentFailureReason or its own CorrelationId. Repeating a rejected offer returns 409 without incrementing RejectCount. DriverFound events that cannot be stored publish DriverOfferRejectedEvent, including replays of previously rejected offers; there is no separate stale-offer guard in the consumer. Payment failure details remain in Payment and logs.

OfferSequence is removed from Trip, Matching sessions, attempt history, DTOs and integration events. OfferedDriverId identifies the current invitation. RejectCount and MaxAllowedRejections still control the rejection limit; users explicitly call Matching again after a rejection.

OfferId is removed from requests, events, driver history and reservations. Accept/reject requests contain only driverId. Matching reuses OfferedDriverId while a reply is pending; reservations and releases are scoped to DriverId + TripId.

Trip owns the rejection-limit decision. DriverRejectEvent carries TripFailed; Matching records the rejected driver, releases the reservation, and marks NoDriver only when TripFailed is true. Finder errors return Result.Failure directly; no-driver results leave the session Searching so users can retry manually. Reject responses expose rejectCount and maxAllowedRejections; the count alias is removed.

RabbitMQ endpoints use durable classic queues with AutoDelete=false. Pending messages remain queued while a service is offline and are removed after a successful acknowledgement. Application endpoints no longer use stream retention or offsets; application retry and transactional outbox remain enabled.
