#!/bin/bash

# Kafka Connection Test Script
# This script tests if Kafka is accessible on localhost:19092

echo "=========================================="
echo "Kafka Connection Test"
echo "=========================================="
echo ""

# Test 1: Check if port 19092 is listening
echo "Test 1: Checking if port 19092 is listening..."
if nc -z localhost 19092 2>/dev/null; then
    echo "✅ Port 19092 is accessible"
else
    echo "❌ Port 19092 is NOT accessible"
    echo "   Make sure Kafka is running: docker-compose -f docker-compose-kafka.yml up -d"
    exit 1
fi
echo ""

# Test 2: Test Kafka connection using kafka-console-producer (if available)
echo "Test 2: Testing Kafka connection..."
if command -v kafka-console-producer &> /dev/null; then
    echo "kafka-console-producer found, testing connection..."
    timeout 5 kafka-console-producer --bootstrap-server localhost:19092 --topic test-connection 2>&1 | head -1
    if [ $? -eq 0 ]; then
        echo "✅ Kafka connection successful"
    else
        echo "❌ Kafka connection failed"
    fi
else
    echo "⚠️  kafka-console-producer not found, skipping producer test"
fi
echo ""

# Test 3: Check Docker container status
echo "Test 3: Checking Kafka Docker container status..."
if docker ps | grep -q kafka; then
    echo "✅ Kafka container is running"
    docker ps | grep kafka
else
    echo "❌ Kafka container is NOT running"
    echo "   Start Kafka: docker-compose -f docker-compose-kafka.yml up -d"
fi
echo ""

# Test 4: Check Kafka logs for connection errors
echo "Test 4: Checking Kafka logs for errors..."
if docker logs kafka --tail 20 2>&1 | grep -i "error\|exception\|failed" > /dev/null; then
    echo "⚠️  Found errors in Kafka logs:"
    docker logs kafka --tail 20 2>&1 | grep -i "error\|exception\|failed"
else
    echo "✅ No errors found in Kafka logs"
fi
echo ""

echo "=========================================="
echo "Test Complete"
echo "=========================================="
echo ""
echo "If all tests pass, your services should be able to connect to Kafka on localhost:19092"
echo ""

