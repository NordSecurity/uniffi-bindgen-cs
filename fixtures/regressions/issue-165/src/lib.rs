/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use std::sync::Arc;

#[derive(Debug, thiserror::Error, uniffi::Error)]
pub enum AsyncCallbackError {
    #[error("Unexpected")]
    Unexpected,
}

#[uniffi::export(with_foreign)]
#[async_trait::async_trait]
pub trait AsyncCallback: Send + Sync {
    async fn do_async(&self, v: String) -> String;
    async fn do_async_void(&self, v: String);
    async fn do_async_throws(&self, v: String) -> Result<String, AsyncCallbackError>;
    async fn do_async_void_throws(&self, v: String) -> Result<(), AsyncCallbackError>;
}

#[uniffi::export]
pub async fn call_do_async(callback: Arc<dyn AsyncCallback>, v: String) -> String {
    callback.do_async(v).await
}

#[uniffi::export]
pub async fn call_do_async_void(callback: Arc<dyn AsyncCallback>, v: String) {
    callback.do_async_void(v).await
}

#[uniffi::export]
pub async fn call_do_async_throws(
    callback: Arc<dyn AsyncCallback>,
    v: String,
) -> Result<String, AsyncCallbackError> {
    callback.do_async_throws(v).await
}

#[uniffi::export]
pub async fn call_do_async_void_throws(
    callback: Arc<dyn AsyncCallback>,
    v: String,
) -> Result<(), AsyncCallbackError> {
    callback.do_async_void_throws(v).await
}

uniffi::setup_scaffolding!();
