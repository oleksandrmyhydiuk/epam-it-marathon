@direction1 @level1
Feature: Level 1 - Group Pre-Order Participants Overview
  As a Reservation Owner
  I want to view all participants who joined my group pre-order, their selected dishes, individual subtotals, and the total amount
  So that I can track the group pre-order progress before submitting it to the restaurant

  @ui @e2e @positive
  Scenario: Owner creates reservation, guests join and add dishes, Owner sees all participants, dishes, quantities, and totals
    Given an authenticated Owner has created a group reservation for 4 guests
    When Guest "Andrii" joins via the invite link and adds 2 of dish #1 and submits their cart
    And Guest "Olena" joins via the invite link and adds 1 of dish #2 without submitting their cart
    And the Owner opens the Group Pre-Order page in the UI
    Then the Owner sees participant "Guest: Andrii" with status "Submitted", 2 of dish #1, and the matching subtotal
    And the Owner sees participant "Guest: Olena" with status "Still choosing", 1 of dish #2, and the matching subtotal
    And the API snapshot total equals the sum of all participants' subtotals

  @api @ui @positive
  Scenario: Total sum equals sum of participants and dynamically updates when a guest modifies their cart
    Given an authenticated Owner has created a group reservation for 4 guests
    And Guest "Taras" has joined and added 1 of dish #1
    When the Owner checks the group sessions snapshot
    Then the group total equals 1 multiplied by the price of dish #1
    When Guest "Taras" adds 2 more of dish #1 to their cart
    Then the updated group total equals 3 multiplied by the price of dish #1
    When Guest "Taras" removes 1 of dish #1 from their cart
    Then the updated group total equals 2 multiplied by the price of dish #1

  @ui @api @positive
  Scenario: Unsubmitted participant is shown with pending badge and participant without dishes is shown with zero subtotal
    Given an authenticated Owner has created a group reservation for 4 guests
    And Guest "Iryna" joins the group without adding any dishes
    And Guest "Maksym" joins the group and adds 1 of dish #1 without submitting
    When the Owner opens the Group Pre-Order page in the UI
    Then participant "Guest: Iryna" is listed with "Still choosing", "No dishes yet", and subtotal "$0.00"
    And participant "Guest: Maksym" is listed with "Still choosing" and 1 of dish #1
    And the warning banner indicates that 2 participants haven't submitted their cart yet

  @ui @api @edge
  Scenario: Group reservation where nobody has joined yet returns empty participants and zero total
    Given an authenticated Owner has created a group reservation for 3 guests
    When the Owner requests the group sessions list before any guest joins
    Then the response status code is 200 with 0 participants and total 0

  @api @security @negative
  Scenario Outline: Only the Owner of the group reservation can view the participants list
    Given an authenticated Owner has created a group reservation for 3 guests
    When "<CallerRole>" requests the group sessions list for that group reservation
    Then the API rejects the request with status code <ExpectedStatusCode>

    Examples:
      | CallerRole      | ExpectedStatusCode |
      | OtherCustomer   | 403                |
      | Waiter          | 403                |
      | GuestSession    | 401                |
      | Unauthenticated | 401                |

  @ui @api @negative
  Scenario: Requesting participants for a non-existent group reservation returns 404 Not Found
    Given an authenticated Owner
    When the Owner requests the group sessions list for non-existent group "non-existent-group-id-999"
    Then the API rejects the request with status code 404

