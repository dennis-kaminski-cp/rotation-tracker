Feature: Create project
  A project groups zero or more services.

  Scenario: Create a project
    When I create a project named "Payments"
    Then the project list contains "Payments"

  Scenario: Project names are unique
    Given a project named "Payments" exists
    When I create a project named "Payments"
    Then I see the error "A project named 'Payments' already exists."

  Scenario: Project name is required
    When I create a project named ""
    Then I see the error "The Name field is required."
