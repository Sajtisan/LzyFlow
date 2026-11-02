# AI Research Plan

**Status:** Research options and evaluation direction aligned with technical-design Revision 8. No model, custom training obligation, fixed dataset size, or mandatory experiment catalogue has been selected. No benchmark results are claimed.

## Objective and product context

Investigate local approaches that balance useful file organization, automatic-action correctness, review, and resource use. The target behavior includes existing-folder routing, broad-root fallback, adaptive nested grouping, document-grounded naming, and avoidance of pointless fragmentation.

The download workflow first establishes completion and allows one minute. Files moved into `Downloads/to-be-deleted` are excluded. The AI must not override these eligibility rules, management scope, global mode, or required approval for optional rename/merge suggestions.

The assessed thesis subset must still be chosen explicitly. A trained neural component is an option, not already a required contribution. Research should permit a simpler method to become the product default when evidence supports it.

## Organization tasks

| Task | Question |
| --- | --- |
| Broad placement | Which configured category/root is appropriate? |
| Existing destination | Does an eligible existing folder fit this file? |
| Relatedness | Which eligible files form a meaningful group? |
| Structural utility | Would a subgroup improve navigation enough to justify added structure? |
| Parent selection | Where in the authorized hierarchy should it belong? |
| Naming | Which recognizable document-derived name describes the group? |
| Uncertainty | Is the evidence sufficient for an automatic action or should it be reviewed? |

A single folder classifier does not automatically solve all of these. Models may share representations or components without needing one monolithic architecture.

## Agreed static and dynamic behavior

Static mode uses existing folders and category roots; no classifier-proposed creation. Dynamic mode also creates useful nested groups and can regroup files after their initial move from Downloads. Confident authorized actions execute automatically; uncertain decisions go to review.

When a narrower destination is not justified but the category is confident, files can collect in its root. Group granularity can grow from a course/project folder into meaningful subgroups. Names retain recognizable information from the shared documents.

There is no universal minimum count that proves a group needs a folder. Exact useful-group criteria, single-file exceptions, depth/rate limits, and scheduling are open. A related-download quiet period is a proposal, not an agreed timer. It must not be confused with the confirmed one-minute download grace period.

Initial setup obtains the global mode and independent existing-file adoption choices per destination. Both TUI and fully noninteractive CLI setup/operation are product requirements. Per-destination mode overrides and approval-based rename/merge suggestions are optional extensions.

## Decision models and Jev

The user's interest is a lightweight local model that makes structured decisions instead of generating conversational text. A **local Jev-style decision model is an explicit primary candidate**, not an unnamed synonym for embeddings plus an MLP.

A question-conditioned evaluator could read normalized document/group context and supplied action/name alternatives, then return choices or scores about fit, useful grouping, creation, or review. This can be pretrained, task-adapted, or compared with simpler scorers.

Official Jev is a relevant external reference for the structured-question/decision pattern. Its public documentation describes Choice, Score, and yes/no judgments through an API. A suitable downloadable local counterpart must be separately identified and checked for license, runtime requirements, task quality, and calibration. This plan does not select hosted Jev or authorize cloud inference.

Do not claim that a frozen encoder plus scorer reproduces Jev's undisclosed internal architecture, training, or general question-conditioned behavior. A schema-valid answer can still be a wrong organizational decision.

## Candidate methods

The entries overlap and can be combined. They are options, not an implementation checklist.

| Candidate | Role and principal trade-off |
| --- | --- |
| Deterministic rules | Reliable explicit routing and a reference baseline; limited general semantic grouping. |
| TF-IDF plus logistic regression/SVM/trees | Efficient learned text/metadata baseline. Fixed-label forms and candidate-scoring forms have different dynamic capabilities. |
| Lexical topic models such as NMF/LDA | Discover vocabulary-based themes; need additional hierarchy, naming, action, and uncertainty policy. |
| Generic pretrained embeddings with similarity | Represent files and candidate descriptions; similarity alone is not filing preference. |
| Folder prototypes/centroids or embedding kNN | Use permitted examples of actual folder contents; need cold-start and erroneous-history handling. |
| Embeddings with hierarchical/density-based grouping | Generate related-group proposals without choosing a fixed number of folders in advance; parameters and a separate creation policy remain necessary. |
| Fixed-output neural classifier | Useful for stable broad categories; needs adaptation to predict genuinely new folder labels. |
| Frozen encoder plus trained classical scorer | Learn candidate/group utility with explicit features and a relatively limited weight-fitting task. |
| Frozen encoder plus compact neural scorer | Learn feature interactions for placement or group actions; encoder footprint and suitable training judgments still matter. |
| Contrastive/dual-encoder fine-tuning | Adapt representations to filing relationships; requires source-group split discipline and enough varied examples. |
| Cross-encoder or entailment-style scorer | Jointly evaluate a file/group and candidate description; potentially useful for selective reranking with per-pair inference cost. |
| Pretrained local Jev-style decision model | Evaluate structured organizational questions/actions directly; concrete model and deployment suitability remain to be verified. |
| Task-adapted local decision model | Specialize those judgments to LzyFlow; training scope and architecture remain open. |
| Graph neural network | Model file/folder relationships explicitly; adds changing-graph and supervision complexity. |
| Local generative model | Can propose labels/actions; requires resource and decision-quality evaluation plus deterministic execution boundaries. |
| Encoder/transformer from random initialization | Maximum control, but language pretraining/data can dominate the project; not selected. |
| End-to-end reinforcement learning | Can study sequential organizational utility, but needs a credible simulator/reward/evaluation design; not implied by the desire to penalize over-creation. |

Previous conversational rankings favored compact learned scorers and local Jev-style evaluators for overall fit. Those were subjective recommendations under assumed resource/data budgets, not measured results or selected requirements.

## Combinable extensions

- Knowledge distillation: use an offline teacher to train a smaller student; runtime need not use the teacher.
- Teacher-generated labels or synthetic examples: optional training data techniques requiring separate provenance and held-out evaluation.
- Learned metadata/text fusion or small expert gates: possible ways to combine filename, content, and origin evidence.
- Correction history, prototypes, adapters, or continual updates: personalization options requiring explicit feedback timing and evaluation.
- Hierarchical decision traversal: a policy that can use several of the candidate models, not necessarily a graph NN.
- Generative naming: optional assistance if extraction/selection cannot produce readable grounded labels.
- Image/video encoders: separate evidence capabilities; text models do not automatically understand visual content.

None of these extensions becomes a thesis requirement merely by appearing here. Hosted teachers or other external processing need a separately chosen data/permission policy.

## Candidate architecture to investigate, not adopt by default

A practical comparison could share normalized evidence, existing-folder/group proposals, and deterministic execution while varying the learned decision component.

One option freezes a pretrained encoder and trains a small scorer over document/candidate representations and metadata. Another uses a classical scorer on the same features. A local question-conditioned decision model is a separate option and need not use that exact representation.

Useful group-level evidence could include internal coherence, separation from neighboring groups, shared course/project identifiers, recognizable name quality, overlap with existing folders, parent context, and structural complexity. These are proposed features, not a finalized creation formula. File count can be evidence without being a universal gate.

A group utility learner needs labels/preferences about proposed organization, not only destination labels for individual files. How those judgments are obtained is part of the research design still to choose.

The full runtime cost includes encoder/tokenizer, extraction, naming/grouping, state/cache, and any learned head. No model-size, CPU-latency, or ONNX/.NET compatibility promise is made before measurements.

## Group discovery, naming, and action policy

Keep these responsibilities conceptually distinguishable:

1. Build bounded evidence from eligible files.
2. Propose connected groups, candidate existing destinations, parents, and names.
3. Score relevance and organizational utility using the selected method.
4. Apply current mode, scope, uncertainty, and approval policy.
5. Execute a fixed validated plan through application services.

HDBSCAN retains minimum-size/density parameters; hierarchical clustering retains linkage/cut choices. Selecting either does not eliminate every threshold or solve the product's navigation objective. Grouping algorithms can propose candidates while a separately evaluated policy decides which groups warrant folders.

Names can be extracted or selected from titles, entities, or representative phrases, retaining recognizable document information. A decision model can select a supplied name without generating it. Exact transformations, language, and whether generative assistance is useful remain open.

Poor existing-folder similarity can indicate missing evidence or ambiguity rather than a missing destination. Evaluate usefulness, parent/name suitability, and overlap before creation. Waiting for evidence can leave a confident broad-root placement intact. A genuinely uncertain proposed action goes to review.

## Data and training discipline

Dataset size, supported types/languages, topic coverage, and final annotation protocol remain open. Several hundred documents were discussed in an earlier draft; that number was not an agreed requirement.

If training is selected, divide related source documents/versions into training/development/calibration/test groups before deriving pairs, hard negatives, prototypes, or synthetic variations. More pairs are not more independent documents. Do not train on held-out documents or their teacher-generated variants.

For untrained systems, still separate development/policy tuning from final testing. Folder/topic-held-out scenarios are needed for claims about unseen semantic groups, distinct from known topics whose initial folder was removed.

Keep evaluator-only gold names, group memberships, actions, and future arrivals out of model inputs. If reviewer feedback or online adaptation is evaluated, log the prediction first and declare when feedback becomes available.

## Proposed comparison design

Select an achievable experiment roster rather than implementing every method. Existing baseline keys such as `b0_root`, `b1_rules`, `b2_semantic`, and `b3_hybrid` may remain for continuity. A neural or Jev-style variant is added only if selected; its immutable backend/configuration identity must be recorded.

A useful comparison could include simple routing/similarity, an adaptive grouping policy without custom training, and one or more learned classical/neural/local-decision alternatives. This is advice, not a new mandatory roster.

Compare:

- existing-destination placement;
- missing specific destinations with broad-root fallback;
- related files accumulating over time;
- broad groups developing into useful nested subgroups;
- similar subjects belonging to different courses/projects;
- duplicate/overlapping or unnecessarily fragmented folders;
- uncertain evidence, naming, and action choices.

Measure model-only quality with fixed proposals separately from complete pipeline behavior. A method unable to handle a case must report that limitation rather than silently remove it from evaluation.

## Sequential evaluation and Docker

Replay a versioned arrival order and initial hierarchy. Each run receives only current and permitted past evidence. Simulate folder registration/regrouping in a non-mutating runner, while testing actual creation/moves/grace/exclusion separately in disposable filesystem state.

Distinguish autonomous closed-loop behavior, where errors affect future decisions, from oracle-corrected and reviewer-assisted tracks. User-approved actions do not count as automatic successes.

The shared Docker environment should record exact source/image/model/data/configuration identities, dependencies, resources, warm-up, cache/state reset, and timing boundaries. Experiment branches are possible but not required. See [Development](development.md#ai-experiments-and-docker-comparison).

## Reporting and selection

For the chosen study, define placement correctness, useful creation/splitting, acceptable names/parents, review, and fragmentation before final testing. Multiple names or structures may be acceptable; exact string equality is not sufficient as the sole standard.

Candidate metrics include automatic precision, coverage, selective risk, per-file/action quality, useful/missed/unnecessary groups, redundancy, hierarchy stability, later reuse, naming acceptability, latency, memory, complete model size, and training effort. Numeric targets and the final roster remain open.

A negative custom-NN result is legitimate. Prefer the product backend supported by measured trade-offs, not the most elaborate architecture. Keep the distinction between the product's required behavior and the assessed thesis scope explicit.

## Open research decisions

The concrete local model, mandatory versus optional training contribution, grouping utility supervision, proposal/naming method, activity policy, limits, corpus, final variants/metrics, resource budget, export/runtime format, and thesis deliverable subset remain undecided.

## Primary references

These explain relevant method families; they do not certify any model for LzyFlow:

- [Sentence-BERT paper](https://aclanthology.org/D19-1410/)
- [Sentence Transformers retrieval and reranking](https://www.sbert.net/examples/sentence_transformer/applications/retrieve_rerank/README.html)
- [Sentence Transformers clustering](https://sbert.net/examples/sentence_transformer/applications/clustering/README.html)
- [SetFit method](https://huggingface.co/docs/setfit/conceptual_guides/setfit)
- [SetFit distillation](https://huggingface.co/docs/setfit/how_to/knowledge_distillation)
- [HDBSCAN parameters](https://scikit-learn.org/stable/modules/generated/sklearn.cluster.HDBSCAN.html)
- [BERTopic topic labels](https://maartengr.github.io/BERTopic/getting_started/topicrepresentation/topicrepresentation.html)
- [TypeSafe Choice](https://docs.typesafe.ai/primitives/choice)
- [TypeSafe model reference](https://docs.typesafe.ai/models)
- [Selective classification paper](https://papers.neurips.cc/paper/2017/hash/4a8423d5e91fda00bb7e46540e2b0cf1-Abstract.html)
