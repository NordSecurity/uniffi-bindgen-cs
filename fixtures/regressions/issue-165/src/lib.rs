/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#[derive(Debug, thiserror::Error, uniffi::Error)]
pub enum AsyncCallbackError {
    #[error("Unexpected")]
    Unexpected,
}

#[uniffi::export(callback_interface)]
#[async_trait::async_trait]
pub trait AsyncCallback: Send + Sync {
    async fn do_async(&self, value: String) -> String;
    async fn do_async_void(&self, value: String);
    async fn do_async_throws(&self, value: String) -> Result<String, AsyncCallbackError>;
    async fn do_async_void_throws(&self, value: String) -> Result<(), AsyncCallbackError>;
}

#[uniffi::export]
pub async fn call_do_async(callback: Box<dyn AsyncCallback>, value: String) -> String {
    callback.do_async(value).await
}

#[uniffi::export]
pub async fn call_do_async_void(callback: Box<dyn AsyncCallback>, value: String) {
    callback.do_async_void(value).await
}

#[uniffi::export]
pub async fn call_do_async_throws(
    callback: Box<dyn AsyncCallback>,
    value: String,
) -> Result<String, AsyncCallbackError> {
    callback.do_async_throws(value).await
}

#[uniffi::export]
pub async fn call_do_async_void_throws(
    callback: Box<dyn AsyncCallback>,
    value: String,
) -> Result<(), AsyncCallbackError> {
    callback.do_async_void_throws(value).await
}

uniffi::setup_scaffolding!();
