
package tradeService

import (
	context "context"
	grpc "google.golang.org/grpc"
	codes "google.golang.org/grpc/codes"
	status "google.golang.org/grpc/status"
)

const _ = grpc.SupportPackageIsVersion9

const (
	TradeService_CreateTrade_FullMethodName = "/tradeService.TradeService/CreateTrade"
)

type TradeServiceClient interface {
	CreateTrade(ctx context.Context, in *CreateTradeRequest, opts ...grpc.CallOption) (*CreateTradeResponse, error)
}

type tradeServiceClient struct {
	cc grpc.ClientConnInterface
}

func NewTradeServiceClient(cc grpc.ClientConnInterface) TradeServiceClient {
	return &tradeServiceClient{cc}
}

func (c *tradeServiceClient) CreateTrade(ctx context.Context, in *CreateTradeRequest, opts ...grpc.CallOption) (*CreateTradeResponse, error) {
	cOpts := append([]grpc.CallOption{grpc.StaticMethod()}, opts...)
	out := new(CreateTradeResponse)
	err := c.cc.Invoke(ctx, TradeService_CreateTrade_FullMethodName, in, out, cOpts...)
	if err != nil {
		return nil, err
	}
	return out, nil
}

type TradeServiceServer interface {
	CreateTrade(context.Context, *CreateTradeRequest) (*CreateTradeResponse, error)
	mustEmbedUnimplementedTradeServiceServer()
}

type UnimplementedTradeServiceServer struct{}

func (UnimplementedTradeServiceServer) CreateTrade(context.Context, *CreateTradeRequest) (*CreateTradeResponse, error) {
	return nil, status.Errorf(codes.Unimplemented, "method CreateTrade not implemented")
}
func (UnimplementedTradeServiceServer) mustEmbedUnimplementedTradeServiceServer() {}
func (UnimplementedTradeServiceServer) testEmbeddedByValue()                      {}

type UnsafeTradeServiceServer interface {
	mustEmbedUnimplementedTradeServiceServer()
}

func RegisterTradeServiceServer(s grpc.ServiceRegistrar, srv TradeServiceServer) {
	if t, ok := srv.(interface{ testEmbeddedByValue() }); ok {
		t.testEmbeddedByValue()
	}
	s.RegisterService(&TradeService_ServiceDesc, srv)
}

func _TradeService_CreateTrade_Handler(srv interface{}, ctx context.Context, dec func(interface{}) error, interceptor grpc.UnaryServerInterceptor) (interface{}, error) {
	in := new(CreateTradeRequest)
	if err := dec(in); err != nil {
		return nil, err
	}
	if interceptor == nil {
		return srv.(TradeServiceServer).CreateTrade(ctx, in)
	}
	info := &grpc.UnaryServerInfo{
		Server:     srv,
		FullMethod: TradeService_CreateTrade_FullMethodName,
	}
	handler := func(ctx context.Context, req interface{}) (interface{}, error) {
		return srv.(TradeServiceServer).CreateTrade(ctx, req.(*CreateTradeRequest))
	}
	return interceptor(ctx, in, info, handler)
}

var TradeService_ServiceDesc = grpc.ServiceDesc{
	ServiceName: "tradeService.TradeService",
	HandlerType: (*TradeServiceServer)(nil),
	Methods: []grpc.MethodDesc{
		{
			MethodName: "CreateTrade",
			Handler:    _TradeService_CreateTrade_Handler,
		},
	},
	Streams:  []grpc.StreamDesc{},
	Metadata: "protos/trade.proto",
}
