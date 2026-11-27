Feature: Create service
  A service belongs to exactly one project.

  Background:
    Given a project named "Payments" exists

  Scenario: Create a service in a project
    When I create a service named "Checkout API" in project "Payments"
    Then project "Payments" lists the service "Checkout API"

  Scenario: Create a service with a description
    When I create a service named "Checkout API" with description "Handles card payments" in project "Payments"
    Then project "Payments" lists the service "Checkout API" with description "Handles card payments"

  Scenario: Service names are unique within a project
    Given project "Payments" has a service named "Checkout API"
    When I create a service named "Checkout API" in project "Payments"
    Then I see the error "A service named 'Checkout API' already exists in project 'Payments'."

  Scenario: The same service name may exist in another project
    Given a project named "Billing" exists
    And project "Billing" has a service named "Checkout API"
    When I create a service named "Checkout API" in project "Payments"
    Then project "Payments" lists the service "Checkout API"

  Scenario: Service name is required
    When I create a service named "" in project "Payments"
    Then I see the error "The Name field is required."

  Scenario: Description is limited to 500 characters
    When I create a service named "Checkout API" with a 501-character description in project "Payments"
    Then I see an error that the description is too long

  Scenario: A new project has no services
    Then project "Payments" lists no services
