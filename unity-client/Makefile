# Hijack Poker — Unity Client
# Usage: make [target]

# ── Unity path detection ─────────────────────────────────────────────
# Override with: UNITY_PATH=/path/to/Unity make build-webgl
UNITY_CANDIDATES := \
	/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity \
	/Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity

ifdef UNITY_PATH
  UNITY := $(UNITY_PATH)
else
  UNITY := $(firstword $(wildcard $(UNITY_CANDIDATES)))
endif

# ── Project paths ────────────────────────────────────────────────────
PROJECT_DIR := $(CURDIR)
BUILDS_DIR  := $(PROJECT_DIR)/Builds
LOGS_DIR    := $(PROJECT_DIR)/Logs

# ── Colors ───────────────────────────────────────────────────────────
GREEN  := \033[32m
RED    := \033[31m
CYAN   := \033[36m
BOLD   := \033[1m
RESET  := \033[0m

# ── Shared helpers ───────────────────────────────────────────────────
define check_unity
	@if [ -z "$(UNITY)" ] || [ ! -f "$(UNITY)" ]; then \
		printf "$(RED)Unity not found.$(RESET)\n"; \
		printf "Set UNITY_PATH or install Unity Hub with version 6000.3.10f1.\n"; \
		exit 1; \
	fi
endef

define run_unity
	@mkdir -p $(LOGS_DIR)
	$(UNITY) \
		-quit \
		-batchmode \
		-nographics \
		-projectPath "$(PROJECT_DIR)" \
		-executeMethod $(1) \
		-logFile "$(2)" \
	&& printf "$(GREEN)$(BOLD)Build succeeded!$(RESET) Output: $(3)\n" \
	|| { printf "$(RED)$(BOLD)Build failed.$(RESET) Check log: $(2)\n"; tail -30 "$(2)" 2>/dev/null; exit 1; }
endef

# ── Targets ──────────────────────────────────────────────────────────
# ── Railway deploy paths ────────────────────────────────────────────
ROOT_DIR     := $(PROJECT_DIR)/..
GATEWAY_DIR  := $(ROOT_DIR)/gateway
SV2_DIR      := $(ROOT_DIR)/serverless-v2
INFRA_DIR    := $(ROOT_DIR)/infrastructure

.PHONY: help backend build-webgl build-desktop build-ios serve test e2e e2e-api e2e-viewer e2e-webgl open clean deploy

help: ## Show available targets
	@printf "$(BOLD)Hijack Poker — Unity Client$(RESET)\n\n"
	@grep -E '^[a-zA-Z0-9_-]+:.*##' $(MAKEFILE_LIST) | \
		awk -F ':.*## ' '{printf "  $(CYAN)%-16s$(RESET) %s\n", $$1, $$2}'
	@echo ""

backend: ## Start backend services (Docker Compose engine profile)
	@printf "$(CYAN)Starting backend services…$(RESET)\n"
	cd "$(ROOT_DIR)" && docker compose --profile engine up

build-webgl: ## Build WebGL player
	$(call check_unity)
	@printf "$(CYAN)Building WebGL…$(RESET)\n"
	@export WEBGL_BUILD_PATH="$(BUILDS_DIR)/WebGL"
	$(call run_unity,WebGLBuilder.Build,$(LOGS_DIR)/webgl-build.log,$(BUILDS_DIR)/WebGL)

build-desktop: ## Build macOS desktop app
	$(call check_unity)
	@printf "$(CYAN)Building Desktop (macOS)…$(RESET)\n"
	@export DESKTOP_BUILD_PATH="$(BUILDS_DIR)/Desktop/HijackPoker.app"
	$(call run_unity,DesktopBuilder.Build,$(LOGS_DIR)/desktop-build.log,$(BUILDS_DIR)/Desktop/HijackPoker.app)

build-ios: ## Export iOS Xcode project
	$(call check_unity)
	@printf "$(CYAN)Building iOS Xcode project…$(RESET)\n"
	@export IOS_BUILD_PATH="$(BUILDS_DIR)/iOS"
	$(call run_unity,iOSBuilder.Build,$(LOGS_DIR)/ios-build.log,$(BUILDS_DIR)/iOS)

serve: ## Serve WebGL build locally (port 8090)
	@python3 "$(PROJECT_DIR)/serve-webgl.py"

test: ## Run EditMode tests via Unity batch mode
	$(call check_unity)
	@printf "$(CYAN)Running EditMode tests…$(RESET)\n"
	@mkdir -p $(LOGS_DIR)
	$(UNITY) \
		-quit \
		-batchmode \
		-nographics \
		-projectPath "$(PROJECT_DIR)" \
		-runTests \
		-testPlatform EditMode \
		-testResults "$(LOGS_DIR)/test-results.xml" \
		-logFile "$(LOGS_DIR)/test.log" \
	&& printf "$(GREEN)$(BOLD)Tests passed!$(RESET)\n" \
	|| { printf "$(RED)$(BOLD)Tests failed.$(RESET) Results: $(LOGS_DIR)/test-results.xml\n"; exit 1; }

e2e: ## Run all Playwright E2E tests
	@printf "$(CYAN)Running E2E tests…$(RESET)\n"
	cd "$(ROOT_DIR)/e2e" && npx playwright test

e2e-api: ## Run API E2E tests only
	@printf "$(CYAN)Running API E2E tests…$(RESET)\n"
	cd "$(ROOT_DIR)/e2e" && npx playwright test --project=api

e2e-viewer: ## Run hand viewer E2E tests only
	@printf "$(CYAN)Running hand viewer E2E tests…$(RESET)\n"
	cd "$(ROOT_DIR)/e2e" && npx playwright test --project=hand-viewer

e2e-webgl: ## Run WebGL smoke E2E tests only
	@printf "$(CYAN)Running WebGL smoke E2E tests…$(RESET)\n"
	cd "$(ROOT_DIR)/e2e" && npx playwright test --project=webgl-smoke

open: ## Open project in Unity Editor
	$(call check_unity)
	@printf "$(CYAN)Opening Unity Editor…$(RESET)\n"
	@"$(UNITY)" -projectPath "$(PROJECT_DIR)" &

clean: ## Remove Builds/ and Logs/ directories
	@rm -rf "$(BUILDS_DIR)" "$(LOGS_DIR)"
	@printf "$(GREEN)Cleaned Builds/ and Logs/$(RESET)\n"

deploy: ## Deploy all services to Railway (requires WebGL build)
	@printf "$(CYAN)Preparing shared/ for backend services…$(RESET)\n"
	@rm -rf "$(SV2_DIR)/services/holdem-processor/shared" && \
		cp -r "$(SV2_DIR)/shared" "$(SV2_DIR)/services/holdem-processor/shared"
	@rm -rf "$(SV2_DIR)/services/cash-game-broadcast/shared" && \
		cp -r "$(SV2_DIR)/shared" "$(SV2_DIR)/services/cash-game-broadcast/shared"
	@printf "$(CYAN)Copying WebGL build to gateway…$(RESET)\n"
	@"$(GATEWAY_DIR)/copy-webgl.sh"
	@printf "$(CYAN)Deploying infrastructure…$(RESET)\n"
	cd "$(ROOT_DIR)" && railway up -d -s elasticmq --path-as-root infrastructure/elasticmq
	cd "$(ROOT_DIR)" && railway up -d -s eventbridge-mock --path-as-root infrastructure/eventbridge-mock
	@printf "$(CYAN)Deploying backend services…$(RESET)\n"
	cd "$(ROOT_DIR)" && railway up -d -s holdem-processor --path-as-root serverless-v2/services/holdem-processor
	cd "$(ROOT_DIR)" && railway up -d -s cash-game-broadcast --path-as-root serverless-v2/services/cash-game-broadcast
	@printf "$(CYAN)Deploying gateway…$(RESET)\n"
	cd "$(ROOT_DIR)" && railway up -d -s gateway --path-as-root gateway
	@printf "$(GREEN)$(BOLD)All services deploying!$(RESET)\n"
	@printf "Gateway: https://gateway-production-4960.up.railway.app\n"
