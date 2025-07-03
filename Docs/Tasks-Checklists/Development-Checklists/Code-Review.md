# Code Review Checklist

*Comprehensive checklist for thorough code reviews.*

## 🎯 General Review Guidelines

- [ ] **Pull Request Overview**
  - [ ] PR description is clear and complete
  - [ ] Links to relevant issues/tasks
  - [ ] Screenshots/demos provided (UI changes)
  - [ ] Breaking changes clearly marked

---

## 🏗️ Architecture & Design

- [ ] **Clean Architecture Compliance**
  - [ ] Proper layer separation maintained
  - [ ] Dependencies point inward
  - [ ] No circular dependencies
  - [ ] Domain logic isolated from infrastructure

- [ ] **Domain-Driven Design**
  - [ ] Aggregates properly designed
  - [ ] Business invariants enforced
  - [ ] Domain events used appropriately
  - [ ] Value objects used where appropriate

- [ ] **Design Patterns**
  - [ ] Appropriate patterns applied
  - [ ] SOLID principles followed
  - [ ] No over-engineering
  - [ ] Pattern usage is justified

---
