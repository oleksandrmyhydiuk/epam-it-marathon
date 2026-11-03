@direction1 @level2
Feature: Level 2 - Confirm Consolidated Group Pre-Order
  As a Reservation Owner
  I want to have my own pre-order session in the group and confirm the consolidated pre-order from all submitted carts
  So that the combined order is sent to the restaurant with status SubmittedByCustomer

  @api @ui @positive
  Scenario: Owner retrieves own guestSessionId, adds own dishes, and confirms consolidated pre-order merging identical dishes and excluding unsubmitted carts
    Given an authenticated Owner has created a group reservation for 4 guests
    And the Owner retrieves their own guestSessionId and submits 1 of dish #1
    And Guest "Andrii" joins, adds 2 of dish #1 and 2 of dish #2, and submits their cart
    And Guest "Olena" joins, adds 5 of dish #1, but does NOT submit their cart
    When the Owner opens the Group Pre-Order page and confirms the consolidated pre-order
    Then the UI status badge changes to "Sent to Restaurant" and the Submit button is disabled
    And the consolidated pre-order contains 3 of dish #1 and 2 of dish #2
    And the restaurant Order status is "SubmittedByCustomer"
    And the Group Reservation status is "Submitted"

  @ui @api @waiter @e2e
  Scenario: Waiter sees the confirmed group pre-order with the exact consolidated dishes and quantities
    Given an authenticated Owner has created a group reservation for 3 guests
    And Guest "Andrii" joins, adds 2 of dish #1 and 1 of dish #2, and submits their cart
    And the Owner confirms the consolidated pre-order
    When the Waiter views the pre-order for the reservation
    Then the Waiter sees 2 of dish #1 and 1 of dish #2 in the reservation order

  @api @negative
  Scenario: Cannot confirm consolidated pre-order when there are no submitted sessions
    Given an authenticated Owner has created a group reservation for 3 guests
    And Guest "Olena" joins and adds 1 of dish #1 without submitting their cart
    When the Owner attempts to submit the consolidated pre-order via API
    Then the API returns 400 Bad Request with message "No submitted guest sessions found."

  @api @negative
  Scenario: Cannot confirm consolidated pre-order when submitted sessions contain no dishes
    Given an authenticated Owner has created a group reservation for 3 guests
    When the Owner attempts to submit the consolidated pre-order when no dishes are submitted
    Then the API returns 400 Bad Request

  @api @negative
  Scenario: Cannot confirm consolidated pre-order when reservation or group reservation does not exist
    Given an authenticated Owner has created a group reservation for 3 guests
    When the Owner attempts to submit the consolidated pre-order for non-existent group "non-existent-group-404"
    Then the API returns 404 Not Found
    When the Owner attempts to submit the consolidated pre-order with non-existent reservationId "non-existent-res-404"
    Then the API returns 404 Not Found

  @api @ui @negative
  Scenario: Cannot confirm an already submitted group pre-order a second time
    Given an authenticated Owner has created a group reservation for 3 guests
    And Guest "Andrii" joins, adds 1 of dish #1, and submits their cart
    And the Owner confirms the consolidated pre-order
    When the Owner attempts to submit the same group pre-order a second time
    Then the API returns 400 Bad Request with message "Group pre-order has already been submitted."

  @api @security @negative
  Scenario Outline: Non-Owner cannot retrieve owner guest-session-id or confirm the consolidated pre-order
    Given an authenticated Owner has created a group reservation for 3 guests
    And Guest "Andrii" joins, adds 1 of dish #1, and submits their cart
    When "<CallerRole>" attempts to get the owner guest-session-id or submit the group pre-order
    Then both API requests are rejected with status code <ExpectedStatusCode>

    Examples:
      | CallerRole      | ExpectedStatusCode |
      | OtherCustomer   | 403                |
      | Waiter          | 403                |
      | Unauthenticated | 401                |

