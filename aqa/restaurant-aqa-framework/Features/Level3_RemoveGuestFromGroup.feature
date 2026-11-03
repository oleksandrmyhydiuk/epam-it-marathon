@direction1 @level3
Feature: Level 3 - Remove Participant Session from Group Pre-Order
  As a Reservation Owner
  I want to remove a guest session from my group pre-order and receive the updated participants list and recalculated total
  So that unwanted or mistaken guest sessions do not affect the group order

  @ui @api @positive
  Scenario: Removing one guest out of several keeps remaining participants and recalculates total
    Given an authenticated Owner has created a group reservation for 4 guests
    And Guest "Andrii" joins, adds 2 of dish #1, and submits their cart
    And Guest "Olena" joins, adds 1 of dish #2, and submits their cart
    When the Owner removes Guest "Andrii" from the group pre-order
    Then only Guest "Olena" remains in the participants list
    And the group total is recalculated to equal only Guest "Olena"'s subtotal

  @api @positive @edge
  Scenario: Removing a guest who already submitted their cart and removing the last participant leaves empty group with total 0
    Given an authenticated Owner has created a group reservation for 3 guests
    And Guest "Andrii" joins, adds 2 of dish #1, and submits their cart
    When the Owner removes the submitted Guest "Andrii" from the group pre-order
    Then the returned participants list is empty and the total is 0

  @api @negative
  Scenario: Removing a guest session that belongs to a different group reservation returns 400 Bad Request
    Given an authenticated Owner has created two independent group reservations Group A and Group B
    And Guest "ForeignGuest" joins Group B
    When the Owner attempts to delete "ForeignGuest" session using Group A's groupReservationId
    Then the API returns 400 Bad Request with message "Guest session does not belong to this group reservation."

  @api @negative
  Scenario: Removing a non-existent guest session returns 404 Not Found
    Given an authenticated Owner has created a group reservation for 3 guests
    When the Owner attempts to delete non-existent guestSessionId "non-existent-session-404"
    Then the API returns 404 Not Found with message containing "not found"

  @api @security @negative
  Scenario Outline: Non-Owner cannot delete a guest session from a group reservation
    Given an authenticated Owner has created a group reservation for 3 guests
    And Guest "Andrii" joins and adds 1 of dish #1
    When "<CallerRole>" attempts to delete Guest "Andrii"'s session
    Then the API rejects the deletion with status code <ExpectedStatusCode>

    Examples:
      | CallerRole      | ExpectedStatusCode |
      | OtherCustomer   | 403                |
      | Waiter          | 403                |
      | GuestSession    | 401                |
      | Unauthenticated | 401                |

  @ui @api @e2e @positive
  Scenario: Full End-to-End flow - Multiple guests add dishes, Owner views, removes one guest, confirms pre-order, and Waiter sees order without removed guest
    Given an authenticated Owner has created a group reservation for 4 guests
    And Guest "Andrii" joins, adds 2 of dish #1, and submits their cart
    And Guest "Olena" joins, adds 1 of dish #2, and submits their cart
    When the Owner opens the Group Pre-Order page, removes Guest "Andrii", and confirms the consolidated pre-order
    Then the confirmed order sent to the restaurant contains 1 of dish #2 and does NOT contain dish #1
    And the Waiter sees the reservation order containing only 1 of dish #2

