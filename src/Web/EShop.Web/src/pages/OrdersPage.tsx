import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getOrders, getOrder, cancelOrder } from '../api/ordersApi';
import { OrderSummary } from '../types';

const STATUS_COLORS: Record<string, string> = {
  Pending:   'bg-yellow-100 text-yellow-800',
  Confirmed: 'bg-blue-100 text-blue-800',
  Shipped:   'bg-purple-100 text-purple-800',
  Delivered: 'bg-green-100 text-green-800',
  Cancelled: 'bg-red-100 text-red-800',
};

export default function OrdersPage() {
  const [page, setPage] = useState(1);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['orders', page],
    queryFn: () => getOrders(page).then(r => r.data),
  });

  const { data: orderDetail, isLoading: detailLoading } = useQuery({
    queryKey: ['order', selectedId],
    queryFn: () => getOrder(selectedId!).then(r => r.data),
    enabled: !!selectedId,
  });

  const cancelMutation = useMutation({
    mutationFn: cancelOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      queryClient.invalidateQueries({ queryKey: ['order', selectedId] });
    },
  });

  if (isLoading) {
    return <div className="text-center py-16 text-gray-500">Loading orders...</div>;
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
      {/* Orders list */}
      <div>
        <h1 className="text-2xl font-bold mb-6">My Orders</h1>

        {data?.items.length === 0 && (
          <p className="text-gray-500 text-sm">No orders yet.</p>
        )}

        <div className="space-y-3">
          {data?.items.map((order: OrderSummary) => (
            <div
              key={order.id}
              onClick={() => setSelectedId(order.id)}
              className={`bg-white rounded-xl shadow p-4 cursor-pointer hover:ring-2 hover:ring-indigo-300 transition ${
                selectedId === order.id ? 'ring-2 ring-indigo-500' : ''
              }`}
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-mono text-gray-400">
                  #{order.id.slice(0, 8)}
                </span>
                <span
                  className={`text-xs px-2 py-1 rounded-full font-medium ${
                    STATUS_COLORS[order.status] ?? 'bg-gray-100 text-gray-600'
                  }`}
                >
                  {order.status}
                </span>
              </div>
              <div className="flex justify-between mt-2 text-sm">
                <span className="text-gray-500">
                  {order.itemCount} item{order.itemCount !== 1 ? 's' : ''}
                </span>
                <span className="font-bold">€{order.totalAmount.toFixed(2)}</span>
              </div>
              <p className="text-xs text-gray-400 mt-1">
                {new Date(order.createdAt).toLocaleDateString()}
              </p>
            </div>
          ))}
        </div>

        {data && data.totalPages > 1 && (
          <div className="flex justify-center items-center gap-3 mt-6">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-3 py-1 border rounded text-sm disabled:opacity-40 hover:bg-gray-100"
            >
              Previous
            </button>
            <span className="text-sm text-gray-500">
              Page {page} of {data.totalPages}
            </span>
            <button
              onClick={() => setPage(p => Math.min(data.totalPages, p + 1))}
              disabled={page === data.totalPages}
              className="px-3 py-1 border rounded text-sm disabled:opacity-40 hover:bg-gray-100"
            >
              Next
            </button>
          </div>
        )}
      </div>

      {/* Order detail */}
      {selectedId && (
        <div className="bg-white rounded-xl shadow p-6">
          {detailLoading ? (
            <p className="text-gray-400 text-sm">Loading...</p>
          ) : orderDetail ? (
            <>
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-bold">Order Details</h2>
                <span
                  className={`text-xs px-2 py-1 rounded-full font-medium ${
                    STATUS_COLORS[orderDetail.status] ?? 'bg-gray-100 text-gray-600'
                  }`}
                >
                  {orderDetail.status}
                </span>
              </div>

              <p className="text-xs font-mono text-gray-400 mb-4">#{orderDetail.id}</p>

              <h3 className="text-sm font-semibold text-gray-600 mb-1">Shipping Address</h3>
              <p className="text-sm text-gray-700 mb-4">
                {orderDetail.shippingAddress.street},{' '}
                {orderDetail.shippingAddress.city}
                <br />
                {orderDetail.shippingAddress.zipCode},{' '}
                {orderDetail.shippingAddress.country}
              </p>

              <h3 className="text-sm font-semibold text-gray-600 mb-2">Items</h3>
              <div className="space-y-1 mb-4">
                {orderDetail.items.map(item => (
                  <div
                    key={item.productId}
                    className="flex justify-between text-sm text-gray-700"
                  >
                    <span>
                      {item.productName} × {item.quantity}
                    </span>
                    <span>€{item.totalPrice.toFixed(2)}</span>
                  </div>
                ))}
              </div>

              <div className="flex justify-between font-bold border-t pt-3">
                <span>Total</span>
                <span>€{orderDetail.totalAmount.toFixed(2)}</span>
              </div>

              {orderDetail.status === 'Pending' && (
                <button
                  onClick={() => cancelMutation.mutate(orderDetail.id)}
                  disabled={cancelMutation.isPending}
                  className="mt-4 w-full border border-red-400 text-red-500 py-2 rounded-lg hover:bg-red-50 disabled:opacity-50 transition-colors text-sm"
                >
                  {cancelMutation.isPending ? 'Cancelling...' : 'Cancel Order'}
                </button>
              )}
            </>
          ) : null}
        </div>
      )}
    </div>
  );
}
