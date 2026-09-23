#!/usr/bin/env python3
"""Local HTTP contract fixture for editor tests only; never used by the player."""
import json
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

class Handler(BaseHTTPRequestHandler):
    def log_message(self, *_):
        pass

    def reply(self, code, body, **headers):
        data = json.dumps(body).encode()
        self.send_response(code)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(data)))
        for name, value in headers.items():
            self.send_header(name.replace('_', '-'), value)
        self.end_headers()
        try:
            self.wfile.write(data)
        except (BrokenPipeError, ConnectionResetError):
            pass

    def authorized(self):
        return self.headers.get('Authorization') == 'Bearer fixture-test-key'

    def do_GET(self):
        if self.path == '/health':
            return self.reply(200, {'status': 'fixture'})
        if not self.authorized():
            return self.reply(401, {'detail': 'fixture-test-key'})
        self.reply(200, {'models': [{'name': 'jev-fixture', 'description': 'Test fixture', 'release_date': '2026-09-23'}]})

    def do_POST(self):
        if not self.authorized():
            return self.reply(401, {'detail': 'fixture-test-key'})
        body = json.loads(self.rfile.read(int(self.headers['Content-Length'])))
        if body.get('model') != 'jev-fixture' or body.get('questions', {}).get('action', {}).get('type') != 'choice':
            return self.reply(422, {'detail': 'invalid contract'})
        if self.path.startswith('/unauthorized/'):
            return self.reply(401, {'detail': 'fixture-test-key'})
        if self.path.startswith('/rate/'):
            return self.reply(429, {'detail': 'fixture-test-key'}, Retry_After='3')
        if self.path.startswith('/slow/'):
            time.sleep(2)
        choices = body['questions']['action']['criteria']
        choice = next(iter(choices))
        self.reply(200, {'model': 'jev-fixture-resolved', 'answers': {'action': {
            'type': 'choice', 'choice': choice, 'confidence': 1,
            'probabilities': {name: int(name == choice) for name in choices}}},
            'usage': {'input_tokens': 50, 'output_tokens': 1},
            'credential_echo_for_redaction_test': 'fixture-test-key'})

ThreadingHTTPServer(('127.0.0.1', 8766), Handler).serve_forever()
