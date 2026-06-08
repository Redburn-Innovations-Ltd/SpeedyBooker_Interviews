Feature: Booking a room
  As someone who needs a room
  I want to browse rooms and book one
  So that the room is reserved for my time slot

  Scenario: Browse rooms, make a booking, and find it again
    When I make a GET request to "/api/rooms"
    Then the response status code should be 200
    And the response should contain "Twin Room"

    When I make a POST request to "/api/bookings" with body:
      """
      {
        "roomId": 3,
        "contactEmail": "cucumber@example.com",
        "checkInDate": "2028-03-01",
        "checkOutDate": "2028-03-03",
        "paymentMethod": "Card"
      }
      """
    Then the response status code should be 201
    And the response field "status" should be "Confirmed"
    And the response field "nights" should be "2"
    And I remember the booking id

    When I make a GET request to "/api/bookings?email=cucumber@example.com"
    Then the response status code should be 200
    And the response should contain "cucumber@example.com"

  Scenario: Find rooms available for a date range
    When I make a GET request to "/api/rooms/availability?startDate=2029-01-01&nights=3"
    Then the response status code should be 200
    And the response should contain "Twin Room"

  Scenario: A booking creates a matching pending payment
    When I make a POST request to "/api/bookings" with body:
      """
      {
        "roomId": 4,
        "contactEmail": "payment-spec@example.com",
        "checkInDate": "2028-04-01",
        "checkOutDate": "2028-04-03",
        "paymentMethod": "Invoice"
      }
      """
    Then the response status code should be 201
    And I remember the booking id

    When I make a GET request to "/api/payments?bookingId={id}"
    Then the response status code should be 200
    And the response should contain "Invoice"
    And the response should contain "Pending"

  Scenario: Cancelling a booking marks it cancelled
    When I make a POST request to "/api/bookings" with body:
      """
      {
        "roomId": 6,
        "contactEmail": "cancel-spec@example.com",
        "checkInDate": "2028-05-01",
        "checkOutDate": "2028-05-02"
      }
      """
    Then the response status code should be 201
    And I remember the booking id

    When I make a POST request to "/api/bookings/{id}/cancel"
    Then the response status code should be 204

    When I make a GET request to "/api/bookings/{id}"
    Then the response field "status" should be "Cancelled"

  Scenario: Double-booking the same room is rejected
    When I make a POST request to "/api/bookings" with body:
      """
      {
        "roomId": 5,
        "contactEmail": "first-spec@example.com",
        "checkInDate": "2028-06-01",
        "checkOutDate": "2028-06-04"
      }
      """
    Then the response status code should be 201

    When I make a POST request to "/api/bookings" with body:
      """
      {
        "roomId": 5,
        "contactEmail": "clash-spec@example.com",
        "checkInDate": "2028-06-02",
        "checkOutDate": "2028-06-05"
      }
      """
    Then the response status code should be 409
