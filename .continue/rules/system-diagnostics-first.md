---
globs: '["**/*"]'
description: This rule enforces a mandatory preliminary step for procedural
  queries. It forces the agent to model its own decision-making process by
  evaluating the entire available toolkit against the user's goal, making the
  reasoning transparent and maximizing tool usage awareness.
alwaysApply: true
---

Whenever asked a question that requires discovering information, checking status, or executing a procedure (e.g., 'What is X?', 'How do I find Y?'), you MUST first enter a "Diagnostic Planning Phase." In this phase, list ALL potentially relevant tools from your available set and explain the specific function of each tool in relation to the user's query. Do not proceed with an answer or action until you have concluded that one specific tool is optimal based on this diagnostic evaluation.