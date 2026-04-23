#!/bin/bash
# ─────────────────────────────────────────────────────────────────────────────
# Tạo Kafka topics cho IOC Platform
# Script này được chạy tự động bởi kafka-init service
# ─────────────────────────────────────────────────────────────────────────────

KAFKA="kafka:9092"

create_topic() {
    local topic=$1
    local partitions=${2:-3}
    local retention_ms=${3:-604800000}  # 7 ngày

    kafka-topics --bootstrap-server $KAFKA \
        --create \
        --if-not-exists \
        --topic "$topic" \
        --partitions "$partitions" \
        --replication-factor 1 \
        --config retention.ms="$retention_ms" \
        --config cleanup.policy=delete
    echo "✅ Topic: $topic"
}

echo "⏳ Waiting for Kafka..."
until kafka-broker-api-versions --bootstrap-server $KAFKA > /dev/null 2>&1; do
    sleep 2
done
echo "✅ Kafka ready"
echo ""

# ── IOC Topics ────────────────────────────────────────────────────────────────
echo "Creating IOC topics..."

# Metrics & Queries
create_topic "ioc.metrics.updated"       3
create_topic "ioc.query.executed"        3

# Dashboard events
create_topic "ioc.dashboard.saved"       3
create_topic "ioc.dashboard.deleted"     3

# Finance plugin
create_topic "ioc.finance.budget-updated"   3
create_topic "ioc.finance.invoice-created"  3

# HR plugin
create_topic "ioc.hr.employee-joined"    3
create_topic "ioc.hr.leave-approved"     3

# Marketing plugin
create_topic "ioc.marketing.campaign-launched" 3

# System
create_topic "ioc.system.errors" 1 2592000000  # 30 ngày

echo ""
echo "✅ All topics created!"
kafka-topics --bootstrap-server $KAFKA --list
